using Microsoft.EntityFrameworkCore;
using University.Application.Mapping;
using University.Core.Entities;
using University.Core.Interfaces.Repositories;
using University.Core.Interfaces.Services;
using University.Core.Rules;
using University.Infrastructure.Data;
using University.Shared.Common;
using University.Shared.DTOs.Grades;

namespace University.Infrastructure.Services;

public class GradeService : IGradeService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ApplicationDbContext _context;

    public GradeService(IUnitOfWork unitOfWork, ApplicationDbContext context)
    {
        _unitOfWork = unitOfWork;
        _context = context;
    }

    public async Task<Result<GradeResponseDto>> SubmitGradeAsync(SubmitGradeRequestDto dto)
    {
        var enrollment = await _context.CourseEnrollments
            .Include(e => e.Section).ThenInclude(s => s!.Course)
            .FirstOrDefaultAsync(e => e.Id == dto.EnrollmentId);
        if (enrollment == null)
        {
            return Result<GradeResponseDto>.NotFound("ENROLLMENT_NOT_FOUND", "Enrollment not found.");
        }

        var existing = await _unitOfWork.GradeRepository.GetByEnrollmentIdAsync(dto.EnrollmentId);
        if (existing != null && existing.IsLocked)
        {
            return Result<GradeResponseDto>.Conflict("GRADE_LOCKED", "Grade is locked and cannot be modified.");
        }

        // Financial hold blocks exam/grades
        var financialHoldRule = new FinancialHoldRule(
            await _context.FinancialRecords.AnyAsync(f =>
                f.StudentId == enrollment.StudentId && f.Balance > 0 &&
                f.Status == University.Shared.Enums.FinancialRecordStatus.ACTIVE));
        if (!await financialHoldRule.IsSatisfiedAsync())
        {
            return Result<GradeResponseDto>.Forbidden("FINANCIAL_HOLD", financialHoldRule.Error);
        }

        Grade grade;
        if (existing == null)
        {
            grade = new Grade { EnrollmentId = dto.EnrollmentId };
            await _unitOfWork.Grades.AddAsync(grade);
        }
        else
        {
            grade = existing;
        }

        grade.MidtermScore = dto.MidtermScore;
        grade.FinalScore = dto.FinalScore;
        grade.TotalScore = GradeCalculation.ComputeTotal(dto.MidtermScore, dto.FinalScore);
        grade.LetterGrade = GradeCalculation.DetermineLetter(grade.TotalScore);
        grade.GradePoints = GradeCalculation.DeterminePoints(grade.TotalScore);
        grade.UpdatedAt = DateTime.UtcNow;

        // Update enrollment status to COMPLETED/FAILED
        enrollment.Status = grade.LetterGrade == University.Shared.Enums.GradeLetter.F
            ? University.Shared.Enums.EnrollmentStatus.FAILED
            : University.Shared.Enums.EnrollmentStatus.COMPLETED;
        enrollment.UpdatedAt = DateTime.UtcNow;

        await _unitOfWork.SaveChangesAsync();

        var response = GradeMapper.ToResponse(grade);
        response.StudentId = enrollment.StudentId;
        response.CourseCode = enrollment.Section?.Course?.CourseCode;
        response.CourseName = enrollment.Section?.Course?.CourseName;

        return Result<GradeResponseDto>.Success(response);
    }

    public async Task<Result<GradeResponseDto>> UpdateGradeAsync(Guid gradeId, UpdateGradeRequestDto dto)
    {
        var grade = await _context.Grades
            .Include(g => g.Enrollment).ThenInclude(e => e!.Section).ThenInclude(s => s!.Course)
            .FirstOrDefaultAsync(g => g.Id == gradeId);
        if (grade == null)
        {
            return Result<GradeResponseDto>.NotFound("GRADE_NOT_FOUND", "Grade not found.");
        }

        if (grade.IsLocked)
        {
            return Result<GradeResponseDto>.Conflict("GRADE_LOCKED", "Grade is locked and cannot be modified.");
        }

        grade.MidtermScore = dto.MidtermScore ?? grade.MidtermScore;
        grade.FinalScore = dto.FinalScore ?? grade.FinalScore;
        grade.TotalScore = dto.TotalScore ?? GradeCalculation.ComputeTotal(grade.MidtermScore, grade.FinalScore);
        grade.LetterGrade = dto.LetterGrade ?? GradeCalculation.DetermineLetter(grade.TotalScore);
        grade.GradePoints = GradeCalculation.DeterminePoints(grade.TotalScore);
        grade.UpdatedAt = DateTime.UtcNow;

        await _unitOfWork.SaveChangesAsync();
        return Result<GradeResponseDto>.Success(GradeMapper.ToResponse(grade));
    }

    public async Task<Result<GradeResponseDto>> LockGradeAsync(Guid gradeId)
    {
        var grade = await _context.Grades.FindAsync(gradeId);
        if (grade == null)
        {
            return Result<GradeResponseDto>.NotFound("GRADE_NOT_FOUND", "Grade not found.");
        }

        grade.IsLocked = true;
        grade.IsActive = true; // grades are never soft-deleted (CONFLICT 8)
        grade.UpdatedAt = DateTime.UtcNow;
        await _unitOfWork.SaveChangesAsync();

        return Result<GradeResponseDto>.Success(GradeMapper.ToResponse(grade));
    }

    public async Task<Result<IEnumerable<GradeResponseDto>>> GetStudentGradesAsync(Guid studentId)
    {
        var grades = await _unitOfWork.GradeRepository.GetByStudentIdAsync(studentId);
        return Result<IEnumerable<GradeResponseDto>>.Success(grades.Select(GradeMapper.ToResponse).ToList());
    }

    public async Task<Result<decimal>> CalculateGPAAsync(Guid studentId)
    {
        var gpa = await _unitOfWork.GradeRepository.CalculateGPAAsync(studentId);
        return Result<decimal>.Success(gpa);
    }
}
