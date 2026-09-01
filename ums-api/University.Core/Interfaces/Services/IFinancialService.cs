using University.Shared.Common;
using University.Shared.DTOs.Finance;

namespace University.Core.Interfaces.Services;

public interface IFinancialService
{
    Task<Result<FinancialRecordResponseDto>> GetBalanceAsync(Guid studentId, Guid? semesterId);
    Task<Result<PaymentResponseDto>> RecordPaymentAsync(PaymentRequestDto dto);
    Task<Result<bool>> ApplyScholarshipAsync(Guid studentId, Guid scholarshipId);
    Task<Result<bool>> RefundAsync(Guid financialId, decimal amount, string reason);
    Task<Result<FinancialRecordResponseDto>> GetFinancialRecordAsync(Guid id);
}
