using University.Shared.Enums;

namespace University.Shared.DTOs.AcademicRecords;

public class AcademicRecordResponseDto
{
    public Guid Id { get; set; }
    public Guid StudentId { get; set; }
    public Guid SemesterId { get; set; }
    public decimal? SemesterGpa { get; set; }
    public decimal? CumulativeGpa { get; set; }
    public int? TotalCredits { get; set; }
    public decimal? TotalPoints { get; set; }
    public AcademicStanding AcademicStatus { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
}
