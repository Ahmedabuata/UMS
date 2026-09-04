using University.Shared.Common;
using University.Shared.Enums;

namespace University.Core.Entities;

public class FinancialRecord : BaseEntity
{
    public Guid StudentId { get; set; }
    public Guid? SemesterId { get; set; }
    public decimal TotalDue { get; set; } = 0;
    public decimal TotalPaid { get; set; } = 0;
    public decimal Balance { get; set; } = 0;
    public FinancialRecordStatus Status { get; set; } = FinancialRecordStatus.ACTIVE;

    public Student? Student { get; set; }
    public Semester? Semester { get; set; }
    public ICollection<Payment> Payments { get; set; } = new List<Payment>();
    public ICollection<FinancialRefund> Refunds { get; set; } = new List<FinancialRefund>();
}
