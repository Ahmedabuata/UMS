namespace University.Shared.DTOs.Prerequisites;

public class CreateCoursePrerequisiteRequestDto
{
    public Guid CourseId { get; set; }
    public Guid PrerequisiteCourseId { get; set; }
    public bool IsMandatory { get; set; } = true;
}
