using University.Shared.Enums;

namespace University.Shared.DTOs.Classrooms;

public class ClassroomResponseDto
{
    public Guid Id { get; set; }
    public Guid? BranchId { get; set; }
    public Guid? BuildingId { get; set; }
    public string RoomNumber { get; set; } = string.Empty;
    public string? BuildingName { get; set; }
    public int Capacity { get; set; }
    public int Floor { get; set; }
    public RoomType RoomType { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
}