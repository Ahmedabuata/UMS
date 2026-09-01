using University.Shared.Enums;

namespace University.Shared.DTOs.GraduationRequests;

public class GraduationRequestResponseDto
{
    public Guid Id { get; set; }
    public Guid StudentId { get; set; }
    public DateTime RequestDate { get; set; }
    public DateOnly? ExpectedGraduationDate { get; set; }
    public GraduationStatus Status { get; set; }
    public ClearanceStatus ClearanceStatus { get; set; }
    public decimal? GpaAtRequest { get; set; }
    public int? TotalCreditsAtRequest { get; set; }
    public string? Notes { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
}
