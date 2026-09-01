namespace University.Shared.DTOs.Majors;

public class MajorResponseDto
{
    public Guid Id { get; set; }
    public Guid DepartmentId { get; set; }
    public string MajorName { get; set; } = string.Empty;
    public string MajorCode { get; set; } = string.Empty;
    public int TotalCreditHours { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
}
