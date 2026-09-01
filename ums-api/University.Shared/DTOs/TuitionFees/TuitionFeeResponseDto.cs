namespace University.Shared.DTOs.TuitionFees;

public class TuitionFeeResponseDto
{
    public Guid Id { get; set; }
    public Guid MajorId { get; set; }
    public string AcademicYear { get; set; } = string.Empty;
    public decimal CreditHourPrice { get; set; }
    public bool IsActive { get; set; }
}
