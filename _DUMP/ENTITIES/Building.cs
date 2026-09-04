using University.Shared.Common;

namespace University.Core.Entities;

public class Building : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;
    public Guid? BranchId { get; set; }
    public Branch? Branch { get; set; }
    public string? Address { get; set; }
    public int Floors { get; set; } = 1;

    public ICollection<Classroom> Classrooms { get; set; } = new List<Classroom>();
}