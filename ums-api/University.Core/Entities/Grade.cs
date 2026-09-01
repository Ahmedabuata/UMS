using University.Shared.Common;
using University.Shared.Enums;

namespace University.Core.Entities;

public class Grade : BaseEntity
{
    public Guid EnrollmentId { get; set; }
    public decimal? MidtermScore { get; set; }
    public decimal? FinalScore { get; set; }
    public decimal? TotalScore { get; set; }
    public GradeLetter? LetterGrade { get; set; }
    public decimal? GradePoints { get; set; }
    public bool IsLocked { get; set; } = false;

    public CourseEnrollment? Enrollment { get; set; }
}
