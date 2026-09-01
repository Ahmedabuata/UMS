using System.Diagnostics;
using Microsoft.Extensions.Logging;

namespace University.Application.Behaviors;

public class LoggingBehavior<TRequest, TResponse>
{
    private readonly ILogger _logger;

    public LoggingBehavior(ILogger logger)
    {
        _logger = logger;
    }

    public async Task<TResponse> HandleAsync(
        TRequest request,
        Func<Task<TResponse>> next,
        CancellationToken cancellationToken = default)
    {
        var requestName = typeof(TRequest).Name;
        var stopwatch = Stopwatch.StartNew();

        _logger.LogInformation("Processing request: {RequestName}", requestName);

        try
        {
            var response = await next();
            stopwatch.Stop();
            _logger.LogInformation(
                "Completed request: {RequestName} in {Elapsed}ms",
                requestName,
                stopwatch.ElapsedMilliseconds);
            return response;
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            _logger.LogError(ex, "Request {RequestName} failed after {Elapsed}ms",
                requestName, stopwatch.ElapsedMilliseconds);
            throw;
        }
    }
}
