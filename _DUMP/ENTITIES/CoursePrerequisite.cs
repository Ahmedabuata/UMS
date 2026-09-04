using University.Shared.Common;

namespace University.Core.Entities;

public class CoursePrerequisite : BaseEntity
{
    public Guid CourseId { get; set; }
    public Guid PrerequisiteCourseId { get; set; }
    public bool IsMandatory { get; set; } = true;

    public Course? Course { get; set; }
    public Course? PrerequisiteCourse { get; set; }
}
