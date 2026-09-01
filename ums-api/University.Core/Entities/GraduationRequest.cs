using University.Shared.Common;
using University.Shared.Enums;

namespace University.Core.Entities;

public class GraduationRequest : BaseEntity
{
    public Guid StudentId { get; set; }
    public DateTime RequestDate { get; set; } = DateTime.UtcNow;
    public DateOnly? ExpectedGraduationDate { get; set; }
    public GraduationStatus Status { get; set; } = GraduationStatus.PENDING;
    public ClearanceStatus ClearanceStatus { get; set; } = ClearanceStatus.PENDING;
    public decimal? GpaAtRequest { get; set; }
    public int? TotalCreditsAtRequest { get; set; }
    public string? Notes { get; set; }

    public Student? Student { get; set; }
}
