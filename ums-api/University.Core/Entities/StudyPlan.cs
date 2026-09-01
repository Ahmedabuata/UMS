using University.Shared.Common;

namespace University.Core.Entities;

public class StudyPlan : BaseEntity
{
    public Guid MajorId { get; set; }
    public Guid CourseId { get; set; }
    public int SemesterNumber { get; set; }
    public bool IsMandatory { get; set; } = true;

    public Major? Major { get; set; }
    public Course? Course { get; set; }
}
