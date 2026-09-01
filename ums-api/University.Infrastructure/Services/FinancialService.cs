using Microsoft.EntityFrameworkCore;
using University.Application.Mapping;
using University.Core.Entities;
using University.Core.Interfaces.Repositories;
using University.Core.Interfaces.Services;
using University.Infrastructure.Data;
using University.Shared.Common;
using University.Shared.Constants;
using University.Shared.DTOs.Finance;
using University.Shared.Enums;

namespace University.Infrastructure.Services;

public class FinancialService : IFinancialService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ApplicationDbContext _context;

    public FinancialService(IUnitOfWork unitOfWork, ApplicationDbContext context)
    {
        _unitOfWork = unitOfWork;
        _context = context;
    }

    public async Task<Result<PaymentResponseDto>> RecordPaymentAsync(PaymentRequestDto dto)
    {
        if (dto.Amount <= 0)
        {
            return Result<PaymentResponseDto>.Validation("INVALID_AMOUNT",
                $"Amount must be greater than {PaymentConstants.MinAmount}.");
        }

        var financial = await _unitOfWork.FinancialRepository.GetByIdWithLockAsync(dto.FinancialId);
        if (financial == null)
        {
            return Result<PaymentResponseDto>.NotFound("FINANCIAL_RECORD_NOT_FOUND", "Financial record not found.");
        }

        if (dto.TransactionId != null &&
            await _context.Payments.AnyAsync(p => p.TransactionId == dto.TransactionId))
        {
            return Result<PaymentResponseDto>.Conflict("TRANSACTION_ID_EXISTS", "Transaction id already used.");
        }

        try
        {
            var result = await _unitOfWork.ExecuteInTransactionAsync(async () =>
            {
                var payment = new Payment
                {
                    FinancialId = dto.FinancialId,
                    Amount = dto.Amount,
                    PaymentMethod = dto.PaymentMethod,
                    PaymentDate = DateTime.UtcNow,
                    TransactionId = dto.TransactionId,
                    Notes = dto.Notes,
                    IsActive = true
                };

                await _unitOfWork.Payments.AddAsync(payment);

                financial.TotalPaid += dto.Amount;
                financial.Balance = financial.TotalDue - financial.TotalPaid;
                if (financial.Balance < 0)
                {
                    financial.Balance = 0;
                }

                financial.Status = financial.Balance <= 0
                    ? FinancialRecordStatus.CLOSED
                    : FinancialRecordStatus.ACTIVE;
                financial.UpdatedAt = DateTime.UtcNow;

                await _unitOfWork.FinancialRecords.UpdateAsync(financial);
                await _unitOfWork.SaveChangesAsync();

                return new PaymentResponseDto
                {
                    Id = payment.Id,
                    FinancialId = payment.FinancialId,
                    Amount = payment.Amount,
                    PaymentMethod = payment.PaymentMethod.ToString(),
                    PaymentDate = payment.PaymentDate,
                    TransactionId = payment.TransactionId,
                    Notes = payment.Notes,
                    NewBalance = financial.Balance,
                    IsPaid = financial.Balance <= 0
                };
            });

            return Result<PaymentResponseDto>.Success(result);
        }
        catch (Exception ex)
        {
            return Result<PaymentResponseDto>.Failure(
                Error.Server("PAYMENT_FAILED", $"Payment failed: {ex.Message}"));
        }
    }

    public async Task<Result<FinancialRecordResponseDto>> GetBalanceAsync(Guid studentId, Guid? semesterId)
    {
        var record = await _unitOfWork.FinancialRepository
            .GetByStudentAndSemesterAsync(studentId, semesterId);
        if (record == null)
        {
            return Result<FinancialRecordResponseDto>.NotFound("FINANCIAL_RECORD_NOT_FOUND", "No financial record found.");
        }

        var withPayments = await _unitOfWork.FinancialRepository.GetWithPaymentsAsync(record.Id);
        return Result<FinancialRecordResponseDto>.Success(FinanceMapper.ToResponse(withPayments!));
    }

    public async Task<Result<bool>> ApplyScholarshipAsync(Guid studentId, Guid scholarshipId)
    {
        var scholarship = await _context.Scholarships.FindAsync(scholarshipId);
        if (scholarship == null)
        {
            return Result<bool>.NotFound("SCHOLARSHIP_NOT_FOUND", "Scholarship not found.");
        }

        // Reduces balance by the discount percentage applied to current balance
        await _unitOfWork.ExecuteInTransactionAsync(async () =>
        {
            var records = await _context.FinancialRecords
                .Where(f => f.StudentId == studentId && f.Balance > 0)
                .ToListAsync();

            foreach (var record in records)
            {
                var discount = decimal.Round(record.Balance * (scholarship.DiscountPercentage / 100m), 2);
                if (scholarship.MaxAmount.HasValue)
                {
                    discount = Math.Min(discount, scholarship.MaxAmount.Value);
                }

                record.Balance -= discount;
                if (record.Balance < 0)
                {
                    record.Balance = 0;
                }

                record.Status = record.Balance <= 0
                    ? FinancialRecordStatus.CLOSED
                    : FinancialRecordStatus.ACTIVE;
                record.UpdatedAt = DateTime.UtcNow;
            }

            await _unitOfWork.SaveChangesAsync();
        });

        return Result<bool>.Success(true);
    }

    public async Task<Result<bool>> RefundAsync(Guid financialId, decimal amount, string reason)
    {
        if (amount <= 0)
        {
            return Result<bool>.Validation("INVALID_AMOUNT", "Refund amount must be positive.");
        }

        var financial = await _unitOfWork.FinancialRepository.GetByIdWithLockAsync(financialId);
        if (financial == null)
        {
            return Result<bool>.NotFound("FINANCIAL_RECORD_NOT_FOUND", "Financial record not found.");
        }

        await _unitOfWork.ExecuteInTransactionAsync(async () =>
        {
            var refund = new FinancialRefund
            {
                FinancialId = financialId,
                Amount = amount,
                Reason = reason,
                Status = RefundStatus.PENDING,
                IsActive = true
            };

            await _unitOfWork.FinancialRefunds.AddAsync(refund);

            financial.TotalPaid -= amount;
            if (financial.TotalPaid < 0)
            {
                financial.TotalPaid = 0;
            }

            financial.Balance = financial.TotalDue - financial.TotalPaid;
            if (financial.Balance < 0)
            {
                financial.Balance = 0;
            }

            financial.Status = FinancialRecordStatus.ACTIVE;
            financial.UpdatedAt = DateTime.UtcNow;

            await _unitOfWork.FinancialRecords.UpdateAsync(financial);
            await _unitOfWork.SaveChangesAsync();
        });

        return Result<bool>.Success(true);
    }

    public async Task<Result<FinancialRecordResponseDto>> GetFinancialRecordAsync(Guid id)
    {
        var record = await _unitOfWork.FinancialRepository.GetWithPaymentsAsync(id);
        if (record == null)
        {
            return Result<FinancialRecordResponseDto>.NotFound("FINANCIAL_RECORD_NOT_FOUND", "Financial record not found.");
        }

        return Result<FinancialRecordResponseDto>.Success(FinanceMapper.ToResponse(record));
    }
}
