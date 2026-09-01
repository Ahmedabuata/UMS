namespace University.Shared.Exceptions;

public class ValidationException : Exception
{
    public IDictionary<string, string[]>? Errors { get; set; }

    public ValidationException(string message) : base(message) { }

    public ValidationException(string message, IDictionary<string, string[]> errors) : base(message)
    {
        Errors = errors;
    }
}
