namespace University.Shared.DTOs.Buildings;

public class BuildingResponseDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;
    public Guid? BranchId { get; set; }
    public string? BranchName { get; set; }
    public string? BranchCode { get; set; }
    public string? Address { get; set; }
    public int Floors { get; set; }
    public int ClassroomCount { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
    public List<University.Shared.DTOs.Classrooms.ClassroomResponseDto> Classrooms { get; set; } = new();
}