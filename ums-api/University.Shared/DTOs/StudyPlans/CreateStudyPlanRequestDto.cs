namespace University.Shared.DTOs.StudyPlans;

public class CreateStudyPlanRequestDto
{
    public Guid MajorId { get; set; }
    public Guid CourseId { get; set; }
    public int SemesterNumber { get; set; }
    public bool IsMandatory { get; set; } = true;
}
