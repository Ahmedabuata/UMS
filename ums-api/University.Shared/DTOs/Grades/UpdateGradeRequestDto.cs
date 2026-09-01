using University.Shared.Enums;

namespace University.Shared.DTOs.Grades;

public class UpdateGradeRequestDto
{
    public decimal? MidtermScore { get; set; }
    public decimal? FinalScore { get; set; }
    public decimal? TotalScore { get; set; }
    public GradeLetter? LetterGrade { get; set; }
}
