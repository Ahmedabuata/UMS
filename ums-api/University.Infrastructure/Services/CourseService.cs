using Microsoft.EntityFrameworkCore;
using University.Application.Mapping;
using University.Core.Interfaces.Repositories;
using University.Core.Interfaces.Services;
using University.Core.Rules;
using University.Infrastructure.Data;
using University.Shared.Common;
using University.Shared.Constants;
using University.Shared.DTOs.Courses;

namespace University.Infrastructure.Services;

public class CourseService : ICourseService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ApplicationDbContext _context;

    public CourseService(IUnitOfWork unitOfWork, ApplicationDbContext context)
    {
        _unitOfWork = unitOfWork;
        _context = context;
    }

    public async Task<Result<CourseResponseDto>> GetCourseByIdAsync(Guid id)
    {
        var course = await _unitOfWork.CourseRepository.GetWithPrerequisitesAsync(id);
        if (course == null)
        {
            return Result<CourseResponseDto>.NotFound("COURSE_NOT_FOUND", "Course not found.");
        }

        return Result<CourseResponseDto>.Success(CourseMapper.ToResponse(course));
    }

    public async Task<Result<CourseResponseDto>> CreateCourseAsync(CreateCourseRequestDto dto)
    {
        if (dto.CreditHours < CourseConstants.MinCredit || dto.CreditHours > CourseConstants.MaxCredit)
        {
            return Result<CourseResponseDto>.Validation("INVALID_CREDIT_HOURS",
                $"Credit hours must be between {CourseConstants.MinCredit} and {CourseConstants.MaxCredit}.");
        }

        if (await _context.Courses.AnyAsync(c => c.CourseCode == dto.CourseCode))
        {
            return Result<CourseResponseDto>.Conflict("COURSE_CODE_EXISTS", "Course code already exists.");
        }

        var course = new University.Core.Entities.Course
        {
            CourseCode = dto.CourseCode,
            CourseName = dto.CourseName,
            Description = dto.Description,
            CreditHours = dto.CreditHours,
            LectureHours = dto.LectureHours ?? 3,
            LabHours = dto.LabHours ?? 0,
            MaxStudents = dto.MaxStudents ?? 40,
            IsActive = true
        };

        await _unitOfWork.Courses.AddAsync(course);
        await _unitOfWork.SaveChangesAsync();

        var saved = await _unitOfWork.CourseRepository.GetWithPrerequisitesAsync(course.Id);
        return Result<CourseResponseDto>.Success(CourseMapper.ToResponse(saved!));
    }

    public async Task<Result<CourseResponseDto>> UpdateCourseAsync(Guid id, UpdateCourseRequestDto dto)
    {
        var course = await _context.Courses.FindAsync(id);
        if (course == null)
        {
            return Result<CourseResponseDto>.NotFound("COURSE_NOT_FOUND", "Course not found.");
        }

        if (dto.CourseName != null) course.CourseName = dto.CourseName;
        if (dto.Description != null) course.Description = dto.Description;
        if (dto.CreditHours.HasValue) course.CreditHours = dto.CreditHours.Value;
        if (dto.MaxStudents.HasValue) course.MaxStudents = dto.MaxStudents.Value;
        if (dto.IsActive.HasValue) course.IsActive = dto.IsActive.Value;
        course.UpdatedAt = DateTime.UtcNow;

        await _unitOfWork.Courses.UpdateAsync(course);
        await _unitOfWork.SaveChangesAsync();

        var saved = await _unitOfWork.CourseRepository.GetWithPrerequisitesAsync(id);
        return Result<CourseResponseDto>.Success(CourseMapper.ToResponse(saved!));
    }

    public async Task<Result<bool>> DeleteCourseAsync(Guid id)
    {
        var course = await _context.Courses
            .Include(c => c.Sections)
            .FirstOrDefaultAsync(c => c.Id == id);
        if (course == null)
        {
            return Result<bool>.NotFound("COURSE_NOT_FOUND", "Course not found.");
        }

        var hasActiveEnrollments = await _context.CourseEnrollments
            .AnyAsync(e => e.Section!.CourseId == id &&
                           e.Status == University.Shared.Enums.EnrollmentStatus.ENROLLED &&
                           e.IsActive);

        var rule = new CourseDeletionRule(hasActiveEnrollments);
        if (!await rule.IsSatisfiedAsync())
        {
            return Result<bool>.Conflict("COURSE_HAS_ENROLLMENTS", rule.Error);
        }

        await _unitOfWork.Courses.DeleteAsync(id);
        await _unitOfWork.SaveChangesAsync();
        return Result<bool>.Success(true);
    }

    public async Task<Result<IEnumerable<CourseResponseDto>>> GetAvailableCoursesAsync(Guid studentId)
    {
        var courses = await _unitOfWork.CourseRepository.GetAvailableForStudentAsync(studentId);
        return Result<IEnumerable<CourseResponseDto>>.Success(courses.Select(CourseMapper.ToResponse).ToList());
    }

    public async Task<Result<CourseResponseDto>> GetWithPrerequisitesAsync(Guid id) =>
        await GetCourseByIdAsync(id);

    public async Task<Result<IEnumerable<CourseResponseDto>>> GetAllAsync()
    {
        var courses = await _context.Courses
            .Include(c => c.Prerequisites).ThenInclude(p => p.PrerequisiteCourse)
            .Where(c => c.IsActive)
            .ToListAsync();
        return Result<IEnumerable<CourseResponseDto>>.Success(courses.Select(CourseMapper.ToResponse).ToList());
    }
}
