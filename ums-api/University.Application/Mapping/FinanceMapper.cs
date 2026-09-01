using University.Core.Entities;
using University.Shared.DTOs.Finance;

namespace University.Application.Mapping;

public static class FinanceMapper
{
    public static FinancialRecordResponseDto ToResponse(FinancialRecord record) => new()
    {
        Id = record.Id,
        StudentId = record.StudentId,
        SemesterId = record.SemesterId,
        TotalDue = record.TotalDue,
        TotalPaid = record.TotalPaid,
        Balance = record.Balance,
        Status = record.Status,
        IsActive = record.IsActive,
        CreatedAt = record.CreatedAt,
        Payments = record.Payments.Select(p => new PaymentDto
        {
            Id = p.Id,
            FinancialId = p.FinancialId,
            Amount = p.Amount,
            PaymentMethod = p.PaymentMethod,
            PaymentDate = p.PaymentDate,
            TransactionId = p.TransactionId,
            Notes = p.Notes
        }).ToList()
    };
}
