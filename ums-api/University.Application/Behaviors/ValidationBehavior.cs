using Microsoft.Extensions.Logging;
using University.Shared.Common;
using University.Shared.Exceptions;

namespace University.Application.Behaviors;

public class ValidationBehavior
{
    private readonly ILogger _logger;

    public ValidationBehavior(ILogger logger)
    {
        _logger = logger;
    }

    public void Validate(string requestName, IDictionary<string, string[]>? errors)
    {
        if (errors == null || errors.Count == 0)
        {
            return;
        }

        _logger.LogWarning("Validation failed for {RequestName}: {Errors}", requestName, errors);
        throw new ValidationException("One or more validation errors occurred.", errors);
    }
}
