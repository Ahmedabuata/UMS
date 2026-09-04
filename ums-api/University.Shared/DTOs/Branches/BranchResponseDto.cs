namespace University.Shared.DTOs.Branches;

public class BranchResponseDto
{
    public Guid Id { get; set; }
    public string BranchName { get; set; } = string.Empty;
    public string BranchCode { get; set; } = string.Empty;
    public string? BranchLocation { get; set; }
    public string? BranchDescription { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
}
