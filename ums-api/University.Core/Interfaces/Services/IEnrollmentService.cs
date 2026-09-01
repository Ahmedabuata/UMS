using University.Shared.Common;
using University.Shared.DTOs.Enrollments;

namespace University.Core.Interfaces.Services;

public interface IEnrollmentService
{
    Task<Result<EnrollmentResponseDto>> EnrollAsync(EnrollRequestDto dto);
    Task<Result<bool>> DropAsync(Guid enrollmentId, string? reason);
    Task<Result<IEnumerable<EnrollmentResponseDto>>> GetStudentEnrollmentsAsync(Guid studentId, Guid semesterId);
    Task<Result<bool>> CheckPrerequisitesAsync(Guid studentId, Guid courseId);
}
