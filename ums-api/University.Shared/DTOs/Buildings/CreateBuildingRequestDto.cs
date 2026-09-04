namespace University.Shared.DTOs.Buildings;

public class CreateBuildingRequestDto
{
    public string Name { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;
    public Guid? BranchId { get; set; }
    public string? Address { get; set; }
    public int Floors { get; set; } = 1;
}