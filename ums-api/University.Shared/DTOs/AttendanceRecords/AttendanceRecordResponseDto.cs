namespace University.Shared.DTOs.AttendanceRecords;

public class AttendanceRecordResponseDto
{
    public Guid Id { get; set; }
    public Guid EnrollmentId { get; set; }
    public DateOnly AttendanceDate { get; set; }
    public bool IsPresent { get; set; }
    public string? Notes { get; set; }
    public bool IsActive { get; set; }
}
