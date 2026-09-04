using University.Shared.Common;
using University.Shared.DTOs.Students;

namespace University.Core.Interfaces.Services;

public interface IStudentService
{
    Task<Result<StudentResponseDto>> GetStudentByIdAsync(Guid id);
    Task<Result<StudentResponseDto>> CreateStudentAsync(CreateStudentRequestDto dto);
    Task<Result<StudentResponseDto>> UpdateStudentAsync(Guid id, UpdateStudentRequestDto dto);
    Task<Result<decimal>> GetStudentGPAAsync(Guid studentId);
    Task<Result<bool>> UpdateGPAAsync(Guid studentId, decimal gpa);
    Task<Result<IEnumerable<StudentResponseDto>>> GetAllAsync();
    Task<Result<string>> PreviewStudentNumberAsync(Guid facultyId, Guid academicDepartmentId);
}
