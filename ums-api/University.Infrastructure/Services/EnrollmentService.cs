using Microsoft.EntityFrameworkCore;
using University.Application.Mapping;
using University.Core.Entities;
using University.Core.Interfaces.Repositories;
using University.Core.Interfaces.Services;
using University.Core.Rules;
using University.Infrastructure.Data;
using University.Shared.Common;
using University.Shared.Constants;
using University.Shared.DTOs.Enrollments;
using University.Shared.Enums;

namespace University.Infrastructure.Services;

public class EnrollmentService : IEnrollmentService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ApplicationDbContext _context;

    public EnrollmentService(IUnitOfWork unitOfWork, ApplicationDbContext context)
    {
        _unitOfWork = unitOfWork;
        _context = context;
    }

    public async Task<Result<EnrollmentResponseDto>> EnrollAsync(EnrollRequestDto dto)
    {
        // Validation
        var student = await _unitOfWork.StudentRepository.GetWithMajorAsync(dto.StudentId);
        if (student == null)
        {
            return Result<EnrollmentResponseDto>.NotFound("STUDENT_NOT_FOUND", "Student not found.");
        }

        var studentActiveRule = new StudentMustBeActiveRule(student);
        if (!await studentActiveRule.IsSatisfiedAsync())
        {
            return Result<EnrollmentResponseDto>.Validation("STUDENT_INACTIVE", studentActiveRule.Error);
        }

        // Financial hold ST-SUSP-FIN
        var hasFinancialHold = await _unitOfWork.FinancialRepository
            .GetByStudentAndSemesterAsync(dto.StudentId, dto.SemesterId) != null &&
            await ExistsOutstandingBalanceAsync(dto.StudentId);
        var financialHoldRule = new FinancialHoldRule(hasFinancialHold);
        if (!await financialHoldRule.IsSatisfiedAsync())
        {
            return Result<EnrollmentResponseDto>.Forbidden("FINANCIAL_HOLD", financialHoldRule.Error);
        }

        var semester = await _unitOfWork.Semesters.GetByIdAsync(dto.SemesterId);
        if (semester == null)
        {
            return Result<EnrollmentResponseDto>.NotFound("SEMESTER_NOT_FOUND", "Semester not found.");
        }

        var semesterValidRule = new SemesterMustBeValidRule(semester);
        if (!await semesterValidRule.IsSatisfiedAsync())
        {
            return Result<EnrollmentResponseDto>.Validation("SEMESTER_INVALID", semesterValidRule.Error);
        }

        // Lock section row FOR UPDATE to prevent over-enrollment concurrency
        var section = await GetSectionWithLockAsync(dto.SectionId);
        if (section == null)
        {
            return Result<EnrollmentResponseDto>.NotFound("SECTION_NOT_FOUND", "Course section not found.");
        }

        var capacityRule = new EnrollmentCapacityRule(section);
        if (!await capacityRule.IsSatisfiedAsync())
        {
            return Result<EnrollmentResponseDto>.Conflict("SECTION_FULL", capacityRule.Error);
        }

        // Duplicate enrollment check
        if (await _unitOfWork.EnrollmentRepository.ExistsAsync(dto.StudentId, dto.SectionId, dto.SemesterId))
        {
            return Result<EnrollmentResponseDto>.Conflict(
                "DUPLICATE_ENROLLMENT", "Student is already enrolled in this section for this semester.");
        }

        // Prerequisites
        var prerequisites = await _context.CoursePrerequisites
            .Where(p => p.CourseId == section.CourseId && p.IsMandatory)
            .ToListAsync();
        var prerequisiteCheck = await CheckPrerequisitesCoreAsync(dto.StudentId, prerequisites.Select(p => p.PrerequisiteCourseId).ToList());
        if (!prerequisiteCheck)
        {
            return Result<EnrollmentResponseDto>.Validation("PREREQUISITES_NOT_MET",
                "Student has not completed all mandatory prerequisites for this course.");
        }

        // Active enrollments in this semester and their credit total
        var activeEnrollments = await _context.CourseEnrollments
            .Include(e => e.Section!).ThenInclude(s => s!.Course)
            .Where(e => e.StudentId == dto.StudentId &&
                        e.SemesterId == dto.SemesterId &&
                        e.Status == EnrollmentStatus.ENROLLED &&
                        e.IsActive)
            .ToListAsync();

        var currentCredits = activeEnrollments.Sum(e => e.Section!.Course!.CreditHours);
        var course = await _context.Courses.FindAsync(section.CourseId);
        var courseCredits = course?.CreditHours ?? 0;

        // Credit hours rule ST-ACTIVE 12-18 (21 for graduate)
        var isGraduate = student.CompletedCredits >= StudentConstants.MaxCreditHours; // heuristic for graduate load
        var loadValidation = CreditHoursRule.Validate(currentCredits + courseCredits, isGraduate);
        if (!loadValidation.IsValid)
        {
            return Result<EnrollmentResponseDto>.Validation("CREDIT_LIMIT_EXCEEDED", loadValidation.Message);
        }

        // Schedule conflict
        var hasConflict = activeEnrollments.Any(e =>
            CreditHoursRule.HasScheduleConflict(
                e.Section!.ScheduleDays, section.ScheduleDays,
                e.Section.StartTime, e.Section.EndTime,
                section.StartTime, section.EndTime));
        if (hasConflict)
        {
            return Result<EnrollmentResponseDto>.Conflict("SCHEDULE_CONFLICT",
                "This section conflicts with another enrolled section's schedule.");
        }

        // Execute atomically
        try
        {
            var result = await _unitOfWork.ExecuteInTransactionAsync(async () =>
            {
                var enrollment = new CourseEnrollment
                {
                    StudentId = dto.StudentId,
                    SectionId = dto.SectionId,
                    SemesterId = dto.SemesterId,
                    EnrollmentDate = DateTime.UtcNow,
                    Status = EnrollmentStatus.ENROLLED,
                    IsActive = true
                };
                await _unitOfWork.CourseEnrollments.AddAsync(enrollment);

                // Increment current_enrollment atomically
                section.CurrentEnrollment += 1;
                section.UpdatedAt = DateTime.UtcNow;
                await _unitOfWork.CourseSections.UpdateAsync(section);

                await _unitOfWork.SaveChangesAsync();

                // Rebuild financial record accounting
                var financial = await _unitOfWork.FinancialRepository
                    .GetByStudentAndSemesterAsync(dto.StudentId, dto.SemesterId);
                if (financial != null)
                {
                    await ApplyEnrollmentCreditChangeAsync(student, financial, courseCredits);
                }

                var created = await _unitOfWork.EnrollmentRepository.GetWithDetailsAsync(enrollment.Id);
                return EnrollmentMapper.ToResponse(created!);
            });

            return Result<EnrollmentResponseDto>.Success(result);
        }
        catch (Exception ex)
        {
            return Result<EnrollmentResponseDto>.Failure(Error.Conflict(
                "ENROLLMENT_FAILED", $"Enrollment failed: {ex.Message}"));
        }
    }

    public async Task<Result<bool>> DropAsync(Guid enrollmentId, string? reason)
    {
        var enrollment = await _unitOfWork.EnrollmentRepository.GetWithDetailsAsync(enrollmentId);
        if (enrollment == null)
        {
            return Result<bool>.NotFound("ENROLLMENT_NOT_FOUND", "Enrollment not found.");
        }

        // Grade lock immutable - cannot drop once grade is locked
        if (enrollment.Grade != null && enrollment.Grade.IsLocked)
        {
            return Result<bool>.Conflict("GRADE_LOCKED", "Cannot drop a course with a locked grade.");
        }

        if (enrollment.Status != EnrollmentStatus.ENROLLED)
        {
            return Result<bool>.Conflict("NOT_ENROLLED", "Enrollment is not in an active state.");
        }

        try
        {
            await _unitOfWork.ExecuteInTransactionAsync(async () =>
            {
                enrollment.Status = EnrollmentStatus.DROPPED;
                enrollment.IsActive = false;
                enrollment.UpdatedAt = DateTime.UtcNow;
                await _unitOfWork.CourseEnrollments.UpdateAsync(enrollment);

                var section = await _context.CourseSections.FindAsync(enrollment.SectionId);
                if (section != null && section.CurrentEnrollment > 0)
                {
                    section.CurrentEnrollment -= 1;
                    section.UpdatedAt = DateTime.UtcNow;
                    await _unitOfWork.CourseSections.UpdateAsync(section);
                }

                await _unitOfWork.SaveChangesAsync();
            });
        }
        catch
        {
            return Result<bool>.Failure(Error.Server("DROP_FAILED", "Failed to drop the course."));
        }

        return Result<bool>.Success(true);
    }

    public async Task<Result<IEnumerable<EnrollmentResponseDto>>> GetStudentEnrollmentsAsync(Guid studentId, Guid semesterId)
    {
        var enrollments = await _unitOfWork.EnrollmentRepository.GetByStudentAndSemesterAsync(studentId, semesterId);
        return Result<IEnumerable<EnrollmentResponseDto>>.Success(enrollments.Select(EnrollmentMapper.ToResponse).ToList());
    }

    public async Task<Result<bool>> CheckPrerequisitesAsync(Guid studentId, Guid courseId)
    {
        var prereqs = await _context.CoursePrerequisites
            .Where(p => p.CourseId == courseId)
            .ToListAsync();
        var result = await CheckPrerequisitesCoreAsync(studentId, prereqs.Select(p => p.PrerequisiteCourseId).ToList());
        return Result<bool>.Success(result);
    }

    private async Task<CourseSection?> GetSectionWithLockAsync(Guid sectionId) =>
        await _context.CourseSections.FromSqlRaw(
            "SELECT * FROM course_sections WHERE \"id\" = {0} FOR UPDATE", sectionId)
            .AsNoTracking()
            .FirstOrDefaultAsync();

    private async Task<bool> ExistsOutstandingBalanceAsync(Guid studentId) =>
        await _context.FinancialRecords.AnyAsync(f =>
            f.StudentId == studentId && f.Balance > 0 &&
            f.Status == FinancialRecordStatus.ACTIVE);

    private async Task<bool> CheckPrerequisitesCoreAsync(Guid studentId, List<Guid> prerequisiteCourseIds)
    {
        if (prerequisiteCourseIds.Count == 0)
        {
            return true;
        }

        var passedGrades = await _context.Grades
            .Include(g => g.Enrollment!).ThenInclude(e => e!.Section)
            .Where(g => g.Enrollment!.StudentId == studentId &&
                        g.LetterGrade.HasValue &&
                        g.LetterGrade != GradeLetter.F &&
                        g.LetterGrade != GradeLetter.I &&
                        g.LetterGrade != GradeLetter.W)
            .ToListAsync();

        var passedCourseIds = passedGrades
            .Where(g => g.Enrollment!.Section != null)
            .Select(g => g.Enrollment!.Section!.CourseId)
            .ToHashSet();

        return prerequisiteCourseIds.All(passedCourseIds.Contains);
    }

    private async Task ApplyEnrollmentCreditChangeAsync(Student student, FinancialRecord financial, int addedCredits)
    {
        // Recalculate financial record based on number of active enrolled credits
        var activeCredits = await _context.CourseEnrollments
            .Include(e => e.Section!).ThenInclude(s => s!.Course)
            .Where(e => e.StudentId == student.Id &&
                        e.SemesterId == financial.SemesterId &&
                        e.Status == EnrollmentStatus.ENROLLED &&
                        e.IsActive)
            .SumAsync(e => (int?)e.Section!.Course!.CreditHours) ?? 0;

        var tuition = await _context.TuitionFees
            .FirstOrDefaultAsync(t => t.MajorId == student.MajorId && t.AcademicYear == financial.SemesterId.ToString());

        if (tuition != null)
        {
            var newDue = activeCredits * tuition.CreditHourPrice;
            financial.TotalDue = newDue;
            financial.Balance = newDue - financial.TotalPaid;
            if (financial.Balance < 0)
            {
                financial.Balance = 0;
            }

            await _unitOfWork.FinancialRecords.UpdateAsync(financial);
        }
    }
}
