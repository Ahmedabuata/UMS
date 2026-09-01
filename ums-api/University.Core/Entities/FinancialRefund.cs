using University.Shared.Common;
using University.Shared.Enums;

namespace University.Core.Entities;

public class FinancialRefund : BaseEntity
{
    public Guid FinancialId { get; set; }
    public decimal Amount { get; set; }
    public string Reason { get; set; } = string.Empty;
    public RefundStatus Status { get; set; } = RefundStatus.PENDING;

    public FinancialRecord? FinancialRecord { get; set; }
}
