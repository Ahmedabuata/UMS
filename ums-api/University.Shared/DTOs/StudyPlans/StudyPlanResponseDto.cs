namespace University.Shared.DTOs.StudyPlans;

public class StudyPlanResponseDto
{
    public Guid Id { get; set; }
    public Guid MajorId { get; set; }
    public Guid CourseId { get; set; }
    public int SemesterNumber { get; set; }
    public bool IsMandatory { get; set; }
    public bool IsActive { get; set; }
}
