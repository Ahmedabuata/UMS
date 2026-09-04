using University.Shared.Common;
using University.Shared.DTOs.Buildings;
using University.Shared.DTOs.Classrooms;

namespace University.Core.Interfaces.Services;

public interface IBuildingService
{
    Task<Result<IEnumerable<BuildingResponseDto>>> GetAllAsync();
    Task<Result<BuildingResponseDto>> GetByIdAsync(Guid id);
    Task<Result<IEnumerable<ClassroomResponseDto>>> GetClassroomsAsync(Guid buildingId);
    Task<Result<BuildingResponseDto>> CreateAsync(CreateBuildingRequestDto dto);
    Task<Result<BuildingResponseDto>> UpdateAsync(Guid id, CreateBuildingRequestDto dto);
    Task<Result<bool>> DeleteAsync(Guid id);
}