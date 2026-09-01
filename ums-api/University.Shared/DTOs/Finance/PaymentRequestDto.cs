using University.Shared.Enums;

namespace University.Shared.DTOs.Finance;

// CANONICAL - CORE CONTRACT
public class PaymentRequestDto
{
    public Guid FinancialId { get; set; }
    public decimal Amount { get; set; }
    public PaymentMethod PaymentMethod { get; set; }
    public string? TransactionId { get; set; }
    public string? Notes { get; set; }
}
