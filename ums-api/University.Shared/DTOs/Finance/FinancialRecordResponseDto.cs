using University.Shared.Enums;

namespace University.Shared.DTOs.Finance;

public class FinancialRecordResponseDto
{
    public Guid Id { get; set; }
    public Guid StudentId { get; set; }
    public Guid? SemesterId { get; set; }
    public decimal TotalDue { get; set; }
    public decimal TotalPaid { get; set; }
    public decimal Balance { get; set; }
    public FinancialRecordStatus Status { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
    public List<PaymentDto> Payments { get; set; } = new();
}

public class PaymentDto
{
    public Guid Id { get; set; }
    public Guid FinancialId { get; set; }
    public decimal Amount { get; set; }
    public PaymentMethod PaymentMethod { get; set; }
    public DateTime PaymentDate { get; set; }
    public string? TransactionId { get; set; }
    public string? Notes { get; set; }
}
