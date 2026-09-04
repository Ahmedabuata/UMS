namespace University.Shared.DTOs.Branches;

public class CreateBranchRequestDto
{
    public string BranchName { get; set; } = string.Empty;
    public string BranchCode { get; set; } = string.Empty;
    public string? BranchLocation { get; set; }
    public string? BranchDescription { get; set; }
}
