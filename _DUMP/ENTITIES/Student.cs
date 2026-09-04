using University.Shared.Common;
using University.Shared.Enums;

namespace University.Core.Entities;

public class Student : BaseEntity
{
    public Guid? MajorId { get; set; }
    public string StudentNumber { get; set; } = string.Empty;
    public decimal Gpa { get; set; } = 0.00m;
    public int CompletedCredits { get; set; } = 0;
    public DateOnly EnrollmentDate { get; set; } = DateOnly.FromDateTime(DateTime.UtcNow);
    public StudentStatus Status { get; set; } = StudentStatus.ST_ACTIVE;

    // Optional link to a user account. Deleting the user leaves the student record intact
    // (UserId is set to NULL on delete). NOT a shared primary key: student.Id != user.Id.
    public Guid? UserId { get; set; }
    public User? User { get; set; }

    public Major? Major { get; set; }
    public ICollection<CourseEnrollment> Enrollments { get; set; } = new List<CourseEnrollment>();
    public ICollection<AcademicRecord> AcademicRecords { get; set; } = new List<AcademicRecord>();
    public ICollection<FinancialRecord> FinancialRecords { get; set; } = new List<FinancialRecord>();
    public ICollection<Guardian> Guardians { get; set; } = new List<Guardian>();
    public ICollection<GraduationRequest> GraduationRequests { get; set; } = new List<GraduationRequest>();
}
