namespace University.Shared.DTOs.Guardians;

public class CreateGuardianRequestDto
{
    public Guid StudentId { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string? Phone { get; set; }
    public string? Email { get; set; }
    public string Relationship { get; set; } = string.Empty;
    public bool IsPrimary { get; set; }
}
