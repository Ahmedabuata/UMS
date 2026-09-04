using University.Shared.Common;
using University.Shared.DTOs.AcademicDepartments;
using University.Shared.DTOs.Faculties;

namespace University.Core.Interfaces.Services;

public interface IFacultyService
{
    Task<Result<IEnumerable<FacultyResponseDto>>> GetAllAsync();
    Task<Result<FacultyResponseDto>> GetByIdAsync(Guid id);
    Task<Result<IEnumerable<AcademicDepartmentResponseDto>>> GetAcademicDepartmentsAsync(Guid facultyId);
    Task<Result<FacultyResponseDto>> CreateAsync(CreateFacultyRequestDto dto);
    Task<Result<FacultyResponseDto>> UpdateAsync(Guid id, CreateFacultyRequestDto dto);
    Task<Result<bool>> DeleteAsync(Guid id);
}