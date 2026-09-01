namespace University.Shared.DTOs.AttendanceRecords;

public class CreateAttendanceRecordRequestDto
{
    public Guid EnrollmentId { get; set; }
    public DateOnly AttendanceDate { get; set; }
    public bool IsPresent { get; set; }
    public string? Notes { get; set; }
}
