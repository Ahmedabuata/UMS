using University.Shared.Common;
using University.Shared.DTOs.AcademicDepartments;

namespace University.Core.Interfaces.Services;

public interface IAcademicDepartmentService
{
    Task<Result<IEnumerable<AcademicDepartmentResponseDto>>> GetAllAsync(Guid? facultyId);
    Task<Result<AcademicDepartmentResponseDto>> GetByIdAsync(Guid id);
    Task<Result<AcademicDepartmentResponseDto>> CreateAsync(CreateAcademicDepartmentRequestDto dto);
    Task<Result<AcademicDepartmentResponseDto>> UpdateAsync(Guid id, CreateAcademicDepartmentRequestDto dto);
    Task<Result<bool>> DeleteAsync(Guid id);
}