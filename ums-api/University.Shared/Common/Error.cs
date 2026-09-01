namespace University.Shared.Common;

public enum ErrorType
{
    None,
    NotFound,
    Validation,
    Conflict,
    Unauthorized,
    Forbidden,
    ServerError
}

public class Error
{
    public string Code { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public ErrorType Type { get; set; } = ErrorType.None;

    public Error() { }

    public Error(string code, string message, ErrorType type = ErrorType.ServerError)
    {
        Code = code;
        Message = message;
        Type = type;
    }

    public static Error NotFound(string code, string message) =>
        new(code, message, ErrorType.NotFound);

    public static Error Validation(string code, string message) =>
        new(code, message, ErrorType.Validation);

    public static Error Conflict(string code, string message) =>
        new(code, message, ErrorType.Conflict);

    public static Error Unauthorized(string code = "UNAUTHORIZED", string message = "Unauthorized.") =>
        new(code, message, ErrorType.Unauthorized);

    public static Error Forbidden(string code = "FORBIDDEN", string message = "Forbidden.") =>
        new(code, message, ErrorType.Forbidden);

    public static Error Server(string code, string message) =>
        new(code, message, ErrorType.ServerError);
}
