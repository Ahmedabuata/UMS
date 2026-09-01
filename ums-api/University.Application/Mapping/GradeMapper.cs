using University.Core.Entities;
using University.Shared.DTOs.Grades;

namespace University.Application.Mapping;

public static class GradeMapper
{
    public static GradeResponseDto ToResponse(Grade grade) => new()
    {
        Id = grade.Id,
        EnrollmentId = grade.EnrollmentId,
        StudentId = grade.Enrollment?.StudentId,
        CourseCode = grade.Enrollment?.Section?.Course?.CourseCode,
        CourseName = grade.Enrollment?.Section?.Course?.CourseName,
        MidtermScore = grade.MidtermScore,
        FinalScore = grade.FinalScore,
        TotalScore = grade.TotalScore,
        LetterGrade = grade.LetterGrade,
        GradePoints = grade.GradePoints,
        IsLocked = grade.IsLocked,
        CreatedAt = grade.CreatedAt
    };
}
