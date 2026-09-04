using University.Shared.Common;
using University.Shared.DTOs.AdministrativeDepartments;

namespace University.Core.Interfaces.Services;

public interface IAdministrativeDepartmentService
{
    Task<Result<AdministrativeDepartmentResponseDto>> GetByIdAsync(Guid id);
    Task<Result<AdministrativeDepartmentResponseDto>> CreateAsync(CreateAdministrativeDepartmentRequestDto dto);
    Task<Result<AdministrativeDepartmentResponseDto>> UpdateAsync(Guid id, UpdateAdministrativeDepartmentRequestDto dto);
    Task<Result<bool>> DeleteAsync(Guid id);
    Task<Result<bool>> RestoreAsync(Guid id);
    Task<Result<IEnumerable<AdministrativeDepartmentResponseDto>>> GetAllAsync();
}
