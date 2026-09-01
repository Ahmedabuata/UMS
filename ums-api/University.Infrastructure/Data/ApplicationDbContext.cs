using Microsoft.EntityFrameworkCore;
using University.Core.Entities;
using University.Shared.Common;

namespace University.Infrastructure.Data;

public class ApplicationDbContext : DbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }

    public DbSet<Branch> Branches => Set<Branch>();
    public DbSet<AdministrativeDepartment> AdministrativeDepartments => Set<AdministrativeDepartment>();
    public DbSet<Role> Roles => Set<Role>();
    public DbSet<Permission> Permissions => Set<Permission>();
    public DbSet<RolePermission> RolePermissions => Set<RolePermission>();
    public DbSet<Faculty> Faculties => Set<Faculty>();
    public DbSet<AcademicDepartment> AcademicDepartments => Set<AcademicDepartment>();
    public DbSet<Major> Majors => Set<Major>();
    public DbSet<Course> Courses => Set<Course>();
    public DbSet<CoursePrerequisite> CoursePrerequisites => Set<CoursePrerequisite>();
    public DbSet<Semester> Semesters => Set<Semester>();
    public DbSet<Classroom> Classrooms => Set<Classroom>();
    public DbSet<User> Users => Set<User>();
    public DbSet<Instructor> Instructors => Set<Instructor>();
    public DbSet<Student> Students => Set<Student>();
    public DbSet<CourseSection> CourseSections => Set<CourseSection>();
    public DbSet<StudyPlan> StudyPlans => Set<StudyPlan>();
    public DbSet<CourseEnrollment> CourseEnrollments => Set<CourseEnrollment>();
    public DbSet<AttendanceRecord> AttendanceRecords => Set<AttendanceRecord>();
    public DbSet<Grade> Grades => Set<Grade>();
    public DbSet<AcademicRecord> AcademicRecords => Set<AcademicRecord>();
    public DbSet<TuitionFee> TuitionFees => Set<TuitionFee>();
    public DbSet<FinancialRecord> FinancialRecords => Set<FinancialRecord>();
    public DbSet<Payment> Payments => Set<Payment>();
    public DbSet<Scholarship> Scholarships => Set<Scholarship>();
    public DbSet<FinancialRefund> FinancialRefunds => Set<FinancialRefund>();
    public DbSet<Guardian> Guardians => Set<Guardian>();
    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();
    public DbSet<SystemNotification> SystemNotifications => Set<SystemNotification>();
    public DbSet<GraduationRequest> GraduationRequests => Set<GraduationRequest>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(ApplicationDbContext).Assembly);
        base.OnModelCreating(modelBuilder);
    }
}
