using University.Shared.Enums;

namespace University.Shared.DTOs.Classrooms;

public class CreateClassroomRequestDto
{
    public Guid BranchId { get; set; }
    public string RoomNumber { get; set; } = string.Empty;
    public string? BuildingName { get; set; }
    public int Capacity { get; set; }
    public RoomType RoomType { get; set; }
}
