using University.Application.Abstractions;
using University.Core.Interfaces.Services;
using University.Shared.Common;
using University.Shared.DTOs.Finance;

namespace University.Application.Finance.Commands;

public class RecordPaymentCommand : ICommand<PaymentResponseDto>
{
    public PaymentRequestDto Dto { get; set; } = new();
}

public class RecordPaymentCommandHandler : ICommandHandler<RecordPaymentCommand, PaymentResponseDto>
{
    private readonly IFinancialService _financialService;

    public RecordPaymentCommandHandler(IFinancialService financialService)
    {
        _financialService = financialService;
    }

    public Task<Result<PaymentResponseDto>> HandleAsync(
        RecordPaymentCommand command, CancellationToken cancellationToken = default)
        => _financialService.RecordPaymentAsync(command.Dto);
}
