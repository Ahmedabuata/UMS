namespace University.Shared.DTOs.Prerequisites;

public class CoursePrerequisiteResponseDto
{
    public Guid Id { get; set; }
    public Guid CourseId { get; set; }
    public Guid PrerequisiteCourseId { get; set; }
    public bool IsMandatory { get; set; }
    public bool IsActive { get; set; }
}
