using University.Shared.Common;

namespace University.Core.Entities;

public class Course : BaseEntity
{
    public string CourseCode { get; set; } = string.Empty;
    public string CourseName { get; set; } = string.Empty;
    public string? Description { get; set; }
    public int CreditHours { get; set; }
    public int? LectureHours { get; set; } = 3;
    public int? LabHours { get; set; } = 0;
    public int? MaxStudents { get; set; } = 40;

    public ICollection<CoursePrerequisite> Prerequisites { get; set; } = new List<CoursePrerequisite>();
    public ICollection<CoursePrerequisite> DependentCourses { get; set; } = new List<CoursePrerequisite>();
    public ICollection<CourseSection> Sections { get; set; } = new List<CourseSection>();
    public ICollection<StudyPlan> StudyPlans { get; set; } = new List<StudyPlan>();
}
