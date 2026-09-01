using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using University.Shared.Common;
using University.Shared.Exceptions;

namespace University.API.Middleware;

public class GlobalExceptionHandler
{
    private readonly RequestDelegate _next;
    private readonly ILogger<GlobalExceptionHandler> _logger;

    public GlobalExceptionHandler(RequestDelegate next, ILogger<GlobalExceptionHandler> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (NotFoundException ex)
        {
            await WriteErrorAsync(context, StatusCodes.Status404NotFound, Error.NotFound("NOT_FOUND", ex.Message));
        }
        catch (ValidationException ex)
        {
            await WriteErrorAsync(context, StatusCodes.Status400BadRequest,
                new Error("VALIDATION_ERROR", ex.Message, ErrorType.Validation));
        }
        catch (ConflictException ex)
        {
            await WriteErrorAsync(context, StatusCodes.Status409Conflict, Error.Conflict("CONFLICT", ex.Message));
        }
        catch (UnauthorizedException ex)
        {
            await WriteErrorAsync(context, StatusCodes.Status401Unauthorized, Error.Unauthorized("UNAUTHORIZED", ex.Message));
        }
        catch (ForbiddenException ex)
        {
            await WriteErrorAsync(context, StatusCodes.Status403Forbidden, Error.Forbidden("FORBIDDEN", ex.Message));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unhandled exception occurred");
            await WriteErrorAsync(context, StatusCodes.Status500InternalServerError,
                Error.Server("SERVER_ERROR", "An unexpected error occurred."));
        }
    }

    private static async Task WriteErrorAsync(HttpContext context, int statusCode, Error error)
    {
        context.Response.StatusCode = statusCode;
        context.Response.ContentType = "application/json";
        await context.Response.WriteAsJsonAsync(new
        {
            statusCode,
            error = new
            {
                error.Code,
                error.Message,
                error.Type
            }
        });
    }
}
