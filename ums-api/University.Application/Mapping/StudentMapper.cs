using University.Core.Entities;
using University.Shared.DTOs.Students;

namespace University.Application.Mapping;

public static class StudentMapper
{
    public static StudentResponseDto ToResponse(Student student) => new()
    {
        Id = student.Id,
        UserId = student.UserId,
        StudentNumber = student.StudentNumber,
        FullName = string.Empty,
        Email = string.Empty,
        MajorId = student.MajorId,
        MajorName = student.Major?.MajorName,
        Gpa = student.Gpa,
        CompletedCredits = student.CompletedCredits,
        Status = student.Status,
        IsActive = student.IsActive,
        CreatedAt = student.CreatedAt
    };
}
