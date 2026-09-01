namespace University.Shared.Common;

public class Result
{
    public bool IsSuccess { get; private set; }
    public Error? Error { get; private set; }

    public bool IsFailure => !IsSuccess;

    protected Result(bool isSuccess, Error? error)
    {
        IsSuccess = isSuccess;
        Error = error;
    }

    public static Result Success() => new(true, null);
    public static Result Failure(Error error) => new(false, error);

    public static Result NotFound(string code, string message) =>
        Failure(Error.NotFound(code, message));

    public static Result Validation(string code, string message) =>
        Failure(Error.Validation(code, message));

    public static Result Conflict(string code, string message) =>
        Failure(Error.Conflict(code, message));

    public static Result Unauthorized(string code = "UNAUTHORIZED") =>
        Failure(Error.Unauthorized(code));

    public static Result Forbidden(string code = "FORBIDDEN") =>
        Failure(Error.Forbidden(code));
}

public class Result<TValue> : Result
{
    public TValue? Value { get; private set; }

    private Result(bool isSuccess, Error? error, TValue? value)
        : base(isSuccess, error)
    {
        Value = value;
    }

    public static Result<TValue> Success(TValue value) => new(true, null, value);

    public static new Result<TValue> Failure(Error error) => new(false, error, default);

    public static new Result<TValue> NotFound(string code, string message) =>
        Failure(Error.NotFound(code, message));

    public static new Result<TValue> Validation(string code, string message) =>
        Failure(Error.Validation(code, message));

    public static new Result<TValue> Conflict(string code, string message) =>
        Failure(Error.Conflict(code, message));

    public static new Result<TValue> Unauthorized(string code = "UNAUTHORIZED") =>
        Failure(Error.Unauthorized(code));

    public static new Result<TValue> Unauthorized(string code, string message) =>
        Failure(Error.Unauthorized(code, message));

    public static new Result<TValue> Forbidden(string code = "FORBIDDEN") =>
        Failure(Error.Forbidden(code));

    public static new Result<TValue> Forbidden(string code, string message) =>
        Failure(Error.Forbidden(code, message));
}
