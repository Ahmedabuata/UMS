using University.Shared.Enums;

namespace University.Shared.DTOs.FinancialRefunds;

public class FinancialRefundResponseDto
{
    public Guid Id { get; set; }
    public Guid FinancialId { get; set; }
    public decimal Amount { get; set; }
    public string Reason { get; set; } = string.Empty;
    public RefundStatus Status { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
}
