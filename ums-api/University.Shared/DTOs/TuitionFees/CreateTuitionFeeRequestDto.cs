namespace University.Shared.DTOs.TuitionFees;

public class CreateTuitionFeeRequestDto
{
    public Guid MajorId { get; set; }
    public string AcademicYear { get; set; } = string.Empty;
    public decimal CreditHourPrice { get; set; }
}
