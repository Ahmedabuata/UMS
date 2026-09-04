namespace University.Shared.DTOs.Security;

public class ValidateIdentifierResponseDto
{
    public bool Found { get; set; }
    public string Type { get; set; } = string.Empty;
    public string Number { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string? Email { get; set; }
    public string? Phone { get; set; }
    public string? Faculty { get; set; }
    public string? Department { get; set; }
    public bool AlreadyLinked { get; set; }
    public string? Message { get; set; }
}