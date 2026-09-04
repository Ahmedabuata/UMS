using University.Core.Entities;
using University.Shared.DTOs.Employees;
using University.Shared.Enums;

namespace University.Application.Mapping;

public static class EmployeeMapper
{
    public static EmployeeResponseDto ToResponse(Employee emp)
    {
        return new EmployeeResponseDto
        {
            Id = emp.Id,
            EmployeeNumber = emp.EmployeeNumber,
            FullName = emp.FullName,
            Email = emp.Email,
            Phone = emp.Phone,
            DepartmentId = emp.DepartmentId,
            DepartmentName = emp.Department?.DepartmentName,
            BranchId = emp.BranchId,
            BranchName = emp.Branch?.BranchName,
            ContractType = emp.ContractType,
            Status = emp.Status,
            HireDate = emp.HireDate,
            IsActive = emp.IsActive,
            // Shared PK: UserId = نفس Id الموظف (نفس UUID)
            UserId = emp.Id,
            AcademicTitle = null,
            Specialization = null,
            FacultyName = null,
            CreatedAt = emp.CreatedAt,
        };
    }

    // نسخة مع Instructor لنظام Shared PK (Instructor.Id == Employee.Id)
    public static EmployeeResponseDto ToResponse(Employee emp, Instructor? instructor)
    {
        var dto = ToResponse(emp);
        if (instructor != null)
        {
            dto.AcademicTitle = AcademicTitleOf(instructor.AcademicRank);
            dto.Specialization = instructor.Specialization;
            dto.FacultyName = instructor.Faculty?.FacultyName;
        }
        return dto;
    }

    private static string AcademicTitleOf(AcademicRank rank) => rank switch
    {
        AcademicRank.LECTURER => "محاضر",
        AcademicRank.ASSISTANT_PROFESSOR => "أستاذ مساعد",
        AcademicRank.ASSOCIATE_PROFESSOR => "أستاذ مشارك",
        AcademicRank.PROFESSOR => "أستاذ دكتور",
        _ => rank.ToString()
    };
}
