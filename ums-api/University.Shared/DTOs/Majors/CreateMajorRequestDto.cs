namespace University.Shared.DTOs.Majors;

public class CreateMajorRequestDto
{
    public Guid DepartmentId { get; set; }
    public string MajorName { get; set; } = string.Empty;
    public string MajorCode { get; set; } = string.Empty;
    public int? TotalCreditHours { get; set; }
}
