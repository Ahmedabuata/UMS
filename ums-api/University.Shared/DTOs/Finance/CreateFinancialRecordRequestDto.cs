namespace University.Shared.DTOs.Finance;

public class CreateFinancialRecordRequestDto
{
    public Guid StudentId { get; set; }
    public Guid SemesterId { get; set; }
    public decimal TotalDue { get; set; }
}
