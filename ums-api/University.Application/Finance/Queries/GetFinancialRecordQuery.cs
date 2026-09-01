using University.Application.Abstractions;
using University.Core.Interfaces.Services;
using University.Shared.Common;
using University.Shared.DTOs.Finance;

namespace University.Application.Finance.Queries;

public class GetFinancialRecordQuery : IQuery<FinancialRecordResponseDto>
{
    public Guid Id { get; set; }
}

public class GetFinancialRecordQueryHandler
    : IQueryHandler<GetFinancialRecordQuery, FinancialRecordResponseDto>
{
    private readonly IFinancialService _financialService;

    public GetFinancialRecordQueryHandler(IFinancialService financialService)
    {
        _financialService = financialService;
    }

    public Task<Result<FinancialRecordResponseDto>> HandleAsync(
        GetFinancialRecordQuery query, CancellationToken cancellationToken = default)
        => _financialService.GetFinancialRecordAsync(query.Id);
}
