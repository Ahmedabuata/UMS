using University.Core.Entities;
using University.Shared.DTOs.Enrollments;

namespace University.Application.Mapping;

public static class EnrollmentMapper
{
    public static EnrollmentResponseDto ToResponse(CourseEnrollment enrollment) => new()
    {
        Id = enrollment.Id,
        StudentId = enrollment.StudentId,
        StudentNumber = enrollment.Student?.StudentNumber,
        StudentName = enrollment.Student?.User?.FullName,
        SectionId = enrollment.SectionId,
        SectionNumber = enrollment.Section?.SectionNumber,
        CourseId = enrollment.Section?.CourseId ?? Guid.Empty,
        CourseCode = enrollment.Section?.Course?.CourseCode,
        CourseName = enrollment.Section?.Course?.CourseName,
        SemesterId = enrollment.SemesterId,
        SemesterName = enrollment.Semester?.SemesterName,
        Status = enrollment.Status,
        EnrollmentDate = enrollment.EnrollmentDate,
        CreatedAt = enrollment.CreatedAt
    };
}
