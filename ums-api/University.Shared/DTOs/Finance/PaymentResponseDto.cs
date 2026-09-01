namespace University.Shared.DTOs.Finance;

public class PaymentResponseDto
{
    public Guid Id { get; set; }
    public Guid FinancialId { get; set; }
    public decimal Amount { get; set; }
    public string PaymentMethod { get; set; } = string.Empty;
    public DateTime PaymentDate { get; set; }
    public string? TransactionId { get; set; }
    public string? Notes { get; set; }
    public decimal NewBalance { get; set; }
    public bool IsPaid { get; set; }
}
