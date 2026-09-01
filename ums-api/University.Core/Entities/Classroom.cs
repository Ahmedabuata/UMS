using University.Shared.Common;
using University.Shared.Enums;

namespace University.Core.Entities;

public class Classroom : BaseEntity
{
    public Guid BranchId { get; set; }
    public string RoomNumber { get; set; } = string.Empty;
    public string? BuildingName { get; set; }
    public int Capacity { get; set; } = 30;
    public RoomType RoomType { get; set; } = RoomType.LECTURE;

    public Branch? Branch { get; set; }
    public ICollection<CourseSection> Sections { get; set; } = new List<CourseSection>();
}
