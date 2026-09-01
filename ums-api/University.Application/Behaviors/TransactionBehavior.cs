using System.Data;
using Microsoft.Extensions.Logging;
using University.Core.Interfaces.Repositories;

namespace University.Application.Behaviors;

public class TransactionBehavior
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger _logger;

    public TransactionBehavior(IUnitOfWork unitOfWork, ILogger logger)
    {
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<TResponse> ExecuteAsync<TResponse>(
        Func<Task<TResponse>> operation,
        IsolationLevel isolationLevel = IsolationLevel.Serializable,
        CancellationToken cancellationToken = default)
    {
        // Operations that do not require DB isolation (e.g. reads) skip transaction
        if (typeof(TResponse) != typeof(Unit))
        {
            return await operation();
        }

        await _unitOfWork.BeginTransactionAsync(isolationLevel);
        try
        {
            var result = await operation();
            await _unitOfWork.CommitAsync();
            return result;
        }
        catch
        {
            await _unitOfWork.RollbackAsync();
            throw;
        }
    }
}

public readonly struct Unit
{
}
