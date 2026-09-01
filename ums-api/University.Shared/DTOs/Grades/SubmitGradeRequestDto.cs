namespace University.Shared.DTOs.Grades;

public class SubmitGradeRequestDto
{
    public Guid EnrollmentId { get; set; }
    public decimal? MidtermScore { get; set; }
    public decimal? FinalScore { get; set; }
}
