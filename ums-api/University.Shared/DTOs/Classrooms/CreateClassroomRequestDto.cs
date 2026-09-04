using University.Shared.Enums;

namespace University.Shared.DTOs.Classrooms;

public class CreateClassroomRequestDto
{
    public Guid? BuildingId { get; set; }
    public string RoomNumber { get; set; } = string.Empty;
    public int Capacity { get; set; } = 30;
    public int Floor { get; set; } = 1;
    public RoomType RoomType { get; set; } = RoomType.LECTURE;
}