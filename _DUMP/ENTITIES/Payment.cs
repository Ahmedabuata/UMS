using University.Shared.Common;
using University.Shared.Enums;

namespace University.Core.Entities;

public class Payment : BaseEntity
{
    public Guid FinancialId { get; set; }
    public decimal Amount { get; set; }
    public PaymentMethod PaymentMethod { get; set; }
    public DateTime PaymentDate { get; set; } = DateTime.UtcNow;
    public string? TransactionId { get; set; }
    public string? Notes { get; set; }

    public FinancialRecord? FinancialRecord { get; set; }
}
