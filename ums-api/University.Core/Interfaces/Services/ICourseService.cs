using University.Shared.Common;
using University.Shared.DTOs.Courses;

namespace University.Core.Interfaces.Services;

public interface ICourseService
{
    Task<Result<CourseResponseDto>> GetCourseByIdAsync(Guid id);
    Task<Result<CourseResponseDto>> CreateCourseAsync(CreateCourseRequestDto dto);
    Task<Result<CourseResponseDto>> UpdateCourseAsync(Guid id, UpdateCourseRequestDto dto);
    Task<Result<bool>> DeleteCourseAsync(Guid id);
    Task<Result<IEnumerable<CourseResponseDto>>> GetAvailableCoursesAsync(Guid studentId);
    Task<Result<CourseResponseDto>> GetWithPrerequisitesAsync(Guid id);
    Task<Result<IEnumerable<CourseResponseDto>>> GetAllAsync();
}
