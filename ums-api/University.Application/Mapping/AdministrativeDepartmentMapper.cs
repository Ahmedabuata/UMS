using University.Core.Entities;
using University.Shared.DTOs.AdministrativeDepartments;

namespace University.Application.Mapping;

public static class AdministrativeDepartmentMapper
{
    public static AdministrativeDepartmentResponseDto ToResponse(AdministrativeDepartment dept) => new()
    {
        Id = dept.Id,
        BranchId = dept.BranchId,
        BranchName = dept.Branch?.BranchName,
        DepartmentName = dept.DepartmentName,
        DepartmentCode = dept.DepartmentCode,
        Description = dept.Description,
        IsActive = dept.IsActive,
        CreatedAt = dept.CreatedAt
    };
}
