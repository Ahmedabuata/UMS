using University.Shared.Common;
using University.Shared.Enums;

namespace University.Core.Entities;

public class AcademicRecord : BaseEntity
{
    public Guid StudentId { get; set; }
    public Guid SemesterId { get; set; }
    public decimal? SemesterGpa { get; set; }
    public decimal? CumulativeGpa { get; set; }
    public int? TotalCredits { get; set; }
    public decimal? TotalPoints { get; set; }
    public AcademicStanding AcademicStatus { get; set; } = AcademicStanding.GOOD_STANDING;

    public Student? Student { get; set; }
    public Semester? Semester { get; set; }
}
