namespace University.Shared.DTOs.Courses;

public class UpdateCourseRequestDto
{
    public string? CourseName { get; set; }
    public string? Description { get; set; }
    public int? CreditHours { get; set; }
    public int? MaxStudents { get; set; }
    public bool? IsActive { get; set; }
}
