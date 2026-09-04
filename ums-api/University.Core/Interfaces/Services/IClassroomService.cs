using University.Shared.Common;
using University.Shared.DTOs.Classrooms;

namespace University.Core.Interfaces.Services;

public interface IClassroomService
{
    Task<Result<ClassroomResponseDto>> GetByIdAsync(Guid id);
    Task<Result<ClassroomResponseDto>> CreateAsync(CreateClassroomRequestDto dto);
    Task<Result<ClassroomResponseDto>> UpdateAsync(Guid id, CreateClassroomRequestDto dto);
    Task<Result<bool>> DeleteAsync(Guid id);
}