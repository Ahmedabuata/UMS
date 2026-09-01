namespace University.Shared.DTOs.Courses;

public class CourseResponseDto
{
    public Guid Id { get; set; }
    public string CourseCode { get; set; } = string.Empty;
    public string CourseName { get; set; } = string.Empty;
    public string? Description { get; set; }
    public int CreditHours { get; set; }
    public int? LectureHours { get; set; }
    public int? LabHours { get; set; }
    public int? MaxStudents { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
    public List<CourseDto> Prerequisites { get; set; } = new();
}

public class CourseDto
{
    public Guid Id { get; set; }
    public string CourseCode { get; set; } = string.Empty;
    public string CourseName { get; set; } = string.Empty;
    public int CreditHours { get; set; }
}
