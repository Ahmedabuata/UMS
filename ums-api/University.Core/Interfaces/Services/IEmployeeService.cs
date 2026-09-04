using University.Shared.Common;
using University.Shared.DTOs.Employees;
using University.Shared.DTOs.Security;

namespace University.Core.Interfaces.Services;

public interface IEmployeeService
{
    Task<Result<IEnumerable<EmployeeResponseDto>>> GetAllAsync(
        string? department = null, string? branch = null,
        string? contractType = null, string? status = null, string? search = null);
    Task<Result<EmployeeResponseDto>> GetByIdAsync(Guid id);
    Task<Result<EmployeeResponseDto>> CreateAsync(CreateEmployeeRequestDto dto);
    Task<Result<EmployeeResponseDto>> UpdateAsync(Guid id, UpdateEmployeeRequestDto dto);
    Task<Result<bool>> DeleteAsync(Guid id, Guid currentUserId);
    Task<Result<EmployeeResponseDto>> DeactivateAsync(Guid id, Guid currentUserId);
    Task<Result<EmployeeResponseDto>> ActivateAsync(Guid id);
    Task<Result<EmployeeResponseDto>> RestoreAsync(Guid id);
    Task<Result<IEnumerable<EmployeeResponseDto>>> GetByAcademicTitleAsync(string? academicTitle = null);
    Task<Result<string>> PreviewEmployeeNumberAsync(Guid administrativeDepartmentId);
    Task<Result<IEnumerable<EmployeeAccountOptionDto>>> GetWithoutUserAccountsAsync();
}