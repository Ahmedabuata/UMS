namespace University.Shared.DTOs.Courses;

public class CreateCourseRequestDto
{
    public string CourseCode { get; set; } = string.Empty;
    public string CourseName { get; set; } = string.Empty;
    public string? Description { get; set; }
    public int CreditHours { get; set; }
    public int? LectureHours { get; set; }
    public int? LabHours { get; set; }
    public int? MaxStudents { get; set; }
}
