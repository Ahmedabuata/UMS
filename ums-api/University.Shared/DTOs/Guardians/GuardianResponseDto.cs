namespace University.Shared.DTOs.Guardians;

public class GuardianResponseDto
{
    public Guid Id { get; set; }
    public Guid StudentId { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string? Phone { get; set; }
    public string? Email { get; set; }
    public string Relationship { get; set; } = string.Empty;
    public bool IsPrimary { get; set; }
    public bool IsActive { get; set; }
}
