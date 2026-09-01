using University.Shared.Enums;

namespace University.Shared.DTOs.Grades;

public class GradeResponseDto
{
    public Guid Id { get; set; }
    public Guid EnrollmentId { get; set; }
    public Guid? StudentId { get; set; }
    public string? CourseCode { get; set; }
    public string? CourseName { get; set; }
    public decimal? MidtermScore { get; set; }
    public decimal? FinalScore { get; set; }
    public decimal? TotalScore { get; set; }
    public GradeLetter? LetterGrade { get; set; }
    public decimal? GradePoints { get; set; }
    public bool IsLocked { get; set; }
    public DateTime CreatedAt { get; set; }
}
