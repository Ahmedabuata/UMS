using University.Shared.Enums;

namespace University.Shared.DTOs.FinancialRefunds;

public class CreateFinancialRefundRequestDto
{
    public Guid FinancialId { get; set; }
    public decimal Amount { get; set; }
    public string Reason { get; set; } = string.Empty;
    public RefundStatus Status { get; set; }
}
