##################################################
# MODEL_CREATING.cs - OnModelCreating + fluent configs
# OnModelCreating delegates to ApplyConfigurationsFromAssembly;
# actual HasKey/HasIndex/HasOne/HasMany/IsUnique/Shared-PK logic
# lives in Data/Configurations/*.cs (38 files).
##################################################

########## ApplicationDbContext.OnModelCreating ##########
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
    public DbSet<Building> Buildings => Set<Building>();
    public DbSet<Semester> Semesters => Set<Semester>();
    public DbSet<Classroom> Classrooms => Set<Classroom>();
    public DbSet<User> Users => Set<User>();
    public DbSet<Instructor> Instructors => Set<Instructor>();
    public DbSet<Employee> Employees => Set<Employee>();
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
    public DbSet<Module> Modules => Set<Module>();
    public DbSet<Group> Groups => Set<Group>();
    public DbSet<UserRole> UserRoles => Set<UserRole>();
    public DbSet<UserGroup> UserGroups => Set<UserGroup>();
    public DbSet<SecuritySettingsOverride> SecuritySettingsOverrides => Set<SecuritySettingsOverride>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(ApplicationDbContext).Assembly);

        // Method 2 - Shared Primary Key 1:1:
        //   users.id == employees.id  (user account for an employee)
        //   instructors.id == employees.id  (instructors are employees)
        // Employee is the principal of both 1:1 relationships (ON DELETE CASCADE from Employee).
        // Students are NOT shared-PK: they link to a user via Student.UserId (SetNull).
        // User/Employee/Instructor ids are assigned in the app (no DB generator).
        modelBuilder.Entity<User>().Property(u => u.Id).ValueGeneratedNever();
        modelBuilder.Entity<Instructor>().Property(i => i.Id).ValueGeneratedNever();
        modelBuilder.Entity<Employee>().Property(e => e.Id).ValueGeneratedNever();
        modelBuilder.Entity<Student>().Property(s => s.Id).ValueGeneratedNever();

        base.OnModelCreating(modelBuilder);
    }
}

########## ALL CONFIGURATION FILES (38) ##########

===== Configurations/AcademicDepartmentConfiguration.cs =====
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using University.Core.Entities;

namespace University.Infrastructure.Data.Configurations;

public class AcademicDepartmentConfiguration : IEntityTypeConfiguration<AcademicDepartment>
{
    public void Configure(EntityTypeBuilder<AcademicDepartment> builder)
    {
        builder.ToTable("academic_departments");
        builder.ConfigureBase();
        builder.Property(e => e.FacultyId).HasColumnName("faculty_id");
        builder.Property(e => e.DepartmentName).HasColumnName("department_name").HasMaxLength(100).IsRequired();
        builder.Property(e => e.DepartmentCode).HasColumnName("department_code").HasMaxLength(20);
        builder.Property(e => e.HeadName).HasColumnName("head_name").HasMaxLength(100);
        builder.Property(e => e.Description).HasColumnName("description").HasMaxLength(500);
        builder.HasIndex(e => e.DepartmentCode).IsUnique();
        builder.HasOne(e => e.Faculty)
            .WithMany(f => f.AcademicDepartments)
            .HasForeignKey(e => e.FacultyId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

===== Configurations/AcademicRecordConfiguration.cs =====
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using University.Core.Entities;
using University.Shared.Enums;

namespace University.Infrastructure.Data.Configurations;

public class AcademicRecordConfiguration : IEntityTypeConfiguration<AcademicRecord>
{
    public void Configure(EntityTypeBuilder<AcademicRecord> builder)
    {
        builder.ToTable("academic_records");
        builder.ConfigureBase();
        builder.Property(e => e.StudentId).HasColumnName("student_id");
        builder.Property(e => e.SemesterId).HasColumnName("semester_id");
        builder.Property(e => e.SemesterGpa).HasColumnName("semester_gpa").HasPrecision(4, 2);
        builder.Property(e => e.CumulativeGpa).HasColumnName("cumulative_gpa").HasPrecision(4, 2);
        builder.Property(e => e.TotalCredits).HasColumnName("total_credits");
        builder.Property(e => e.TotalPoints).HasColumnName("total_points").HasPrecision(6, 2);
        builder.Property(e => e.AcademicStatus).HasColumnName("academic_status")
            .HasVarcharEnumConversion<AcademicStanding>()
            .HasMaxLength(20);
        builder.HasIndex(e => new { e.StudentId, e.SemesterId }).IsUnique();
        builder.HasOne(e => e.Student)
            .WithMany(s => s.AcademicRecords)
            .HasForeignKey(e => e.StudentId)
            .OnDelete(DeleteBehavior.Cascade);
        builder.HasOne(e => e.Semester)
            .WithMany(s => s.AcademicRecords)
            .HasForeignKey(e => e.SemesterId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

===== Configurations/AdministrativeDepartmentConfiguration.cs =====
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using University.Core.Entities;

namespace University.Infrastructure.Data.Configurations;

public class AdministrativeDepartmentConfiguration : IEntityTypeConfiguration<AdministrativeDepartment>
{
    public void Configure(EntityTypeBuilder<AdministrativeDepartment> builder)
    {
        builder.ToTable("administrative_departments");
        builder.ConfigureBase();
        builder.Property(e => e.BranchId).HasColumnName("branch_id");
        builder.Property(e => e.DepartmentName).HasColumnName("department_name").HasMaxLength(100).IsRequired();
        builder.Property(e => e.DepartmentCode).HasColumnName("department_code").HasMaxLength(20).IsRequired();
        builder.Property(e => e.Description).HasColumnName("description").HasMaxLength(255);
        builder.HasIndex(e => e.DepartmentCode).IsUnique();
        builder.HasOne(e => e.Branch)
            .WithMany(b => b.AdministrativeDepartments)
            .HasForeignKey(e => e.BranchId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

===== Configurations/AttendanceRecordConfiguration.cs =====
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using University.Core.Entities;

namespace University.Infrastructure.Data.Configurations;

public class AttendanceRecordConfiguration : IEntityTypeConfiguration<AttendanceRecord>
{
    public void Configure(EntityTypeBuilder<AttendanceRecord> builder)
    {
        builder.ToTable("attendance_records");
        builder.ConfigureBase();
        builder.Property(e => e.EnrollmentId).HasColumnName("enrollment_id");
        builder.Property(e => e.AttendanceDate).HasColumnName("attendance_date").IsRequired();
        builder.Property(e => e.IsPresent).HasColumnName("is_present").IsRequired();
        builder.Property(e => e.Notes).HasColumnName("notes").HasMaxLength(255);
        builder.HasIndex(e => new { e.EnrollmentId, e.AttendanceDate }).IsUnique();
        builder.HasOne(e => e.Enrollment)
            .WithMany(en => en.AttendanceRecords)
            .HasForeignKey(e => e.EnrollmentId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

===== Configurations/AuditLogConfiguration.cs =====
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using University.Core.Entities;

namespace University.Infrastructure.Data.Configurations;

public class AuditLogConfiguration : IEntityTypeConfiguration<AuditLog>
{
    public void Configure(EntityTypeBuilder<AuditLog> builder)
    {
        builder.ToTable("audit_logs");
        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id).HasColumnName("id");
        builder.Property(e => e.UserId).HasColumnName("user_id");
        builder.Property(e => e.Action).HasColumnName("action").HasMaxLength(50).IsRequired();
        builder.Property(e => e.Entity).HasColumnName("entity").HasMaxLength(100);
        builder.Property(e => e.EntityId).HasColumnName("entity_id").HasMaxLength(50);
        builder.Property(e => e.TableName).HasColumnName("table_name").HasMaxLength(50);
        builder.Property(e => e.OldValues).HasColumnName("old_values").HasColumnType("jsonb");
        builder.Property(e => e.NewValues).HasColumnName("new_values").HasColumnType("jsonb");
        builder.Property(e => e.IpAddress).HasColumnName("ip_address").HasMaxLength(45);
        builder.Property(e => e.UserAgent).HasColumnName("user_agent").HasMaxLength(255);
        builder.Property(e => e.BranchCode).HasColumnName("branch_code").HasMaxLength(50);
        builder.Property(e => e.CreatedBy).HasColumnName("created_by");
        builder.Property(e => e.Timestamp).HasColumnName("timestamp");
        builder.Property(e => e.CreatedAt).HasColumnName("created_at");
        builder.HasIndex(e => e.UserId).HasDatabaseName("idx_audit_logs_user");
        builder.HasIndex(e => e.Timestamp).HasDatabaseName("idx_audit_logs_timestamp");
        builder.HasIndex(e => e.BranchCode).HasDatabaseName("idx_audit_logs_branch_code");
        builder.HasOne(e => e.User)
            .WithMany()
            .HasForeignKey(e => e.UserId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}

===== Configurations/BaseEntityConfiguration.cs =====
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using University.Shared.Common;

namespace University.Infrastructure.Data.Configurations;

public static class BaseEntityConfiguration
{
    public static void ConfigureBase<T>(this EntityTypeBuilder<T> builder) where T : BaseEntity
    {
        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id).HasColumnName("id");
        builder.Property(e => e.CreatedAt).HasColumnName("created_at");
        builder.Property(e => e.UpdatedAt).HasColumnName("updated_at");
        builder.Property(e => e.IsActive).HasColumnName("is_active");
    }

    public static PropertyBuilder<TEnum> HasVarcharEnumConversion<TEnum>(
        this PropertyBuilder<TEnum> builder) where TEnum : struct, Enum
    {
        return builder.HasConversion(
            v => v.ToString(),
            v => (TEnum)Enum.Parse(typeof(TEnum), v));
    }

    public static PropertyBuilder<TEnum?> HasVarcharEnumConversion<TEnum>(
        this PropertyBuilder<TEnum?> builder) where TEnum : struct, Enum
    {
        return builder.HasConversion(
            v => v.HasValue ? v.Value.ToString() : null,
            v => v == null ? (TEnum?)null : (TEnum)Enum.Parse(typeof(TEnum), v));
    }
}

===== Configurations/BranchConfiguration.cs =====
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using University.Core.Entities;

namespace University.Infrastructure.Data.Configurations;

public class BranchConfiguration : IEntityTypeConfiguration<Branch>
{
    public void Configure(EntityTypeBuilder<Branch> builder)
    {
        builder.ToTable("branches");
        builder.ConfigureBase();
        builder.Property(e => e.BranchName).HasColumnName("branch_name").HasMaxLength(100).IsRequired();
        builder.Property(e => e.BranchCode).HasColumnName("branch_code").HasMaxLength(20).IsRequired();
        builder.Property(e => e.BranchLocation).HasColumnName("branch_location").HasMaxLength(200);
        builder.Property(e => e.BranchDescription).HasColumnName("branch_description").HasMaxLength(500);
        builder.HasIndex(e => e.BranchCode).IsUnique();
    }
}

===== Configurations/BuildingConfiguration.cs =====
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using University.Core.Entities;

namespace University.Infrastructure.Data.Configurations;

public class BuildingConfiguration : IEntityTypeConfiguration<Building>
{
    public void Configure(EntityTypeBuilder<Building> builder)
    {
        builder.ToTable("buildings");
        builder.ConfigureBase();
        builder.Property(e => e.Name).HasColumnName("name").HasMaxLength(100).IsRequired();
        builder.Property(e => e.Code).HasColumnName("code").HasMaxLength(20).IsRequired();
        builder.Property(e => e.Address).HasColumnName("address").HasMaxLength(255);
        builder.Property(e => e.Floors).HasColumnName("floors");
        builder.Property(e => e.BranchId).HasColumnName("branch_id");
        builder.HasIndex(e => e.Code).IsUnique();

        builder.HasOne(e => e.Branch)
            .WithMany()
            .HasForeignKey(e => e.BranchId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasMany(e => e.Classrooms)
            .WithOne(c => c.Building)
            .HasForeignKey(c => c.BuildingId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
===== Configurations/ClassroomConfiguration.cs =====
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using University.Core.Entities;
using University.Shared.Enums;

namespace University.Infrastructure.Data.Configurations;

public class ClassroomConfiguration : IEntityTypeConfiguration<Classroom>
{
    public void Configure(EntityTypeBuilder<Classroom> builder)
    {
        builder.ToTable("classrooms");
        builder.ConfigureBase();
        builder.Property(e => e.BranchId).HasColumnName("branch_id");
        builder.Property(e => e.BuildingId).HasColumnName("building_id");
        builder.Property(e => e.RoomNumber).HasColumnName("room_number").HasMaxLength(20).IsRequired();
        builder.Property(e => e.BuildingName).HasColumnName("building_name").HasMaxLength(50);
        builder.Property(e => e.Capacity).HasColumnName("capacity");
        builder.Property(e => e.Floor).HasColumnName("floor");
        builder.Property(e => e.RoomType).HasColumnName("room_type")
            .HasVarcharEnumConversion<RoomType>()
            .HasMaxLength(20);
        builder.HasIndex(e => new { e.BranchId, e.RoomNumber }).IsUnique();
        builder.HasOne(e => e.Branch)
            .WithMany(b => b.Classrooms)
            .HasForeignKey(e => e.BranchId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(e => e.Building)
            .WithMany(b => b.Classrooms)
            .HasForeignKey(e => e.BuildingId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
===== Configurations/CourseConfiguration.cs =====
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using University.Core.Entities;

namespace University.Infrastructure.Data.Configurations;

public class CourseConfiguration : IEntityTypeConfiguration<Course>
{
    public void Configure(EntityTypeBuilder<Course> builder)
    {
        builder.ToTable("courses");
        builder.ConfigureBase();
        builder.Property(e => e.CourseCode).HasColumnName("course_code").HasMaxLength(20).IsRequired();
        builder.Property(e => e.CourseName).HasColumnName("course_name").HasMaxLength(150).IsRequired();
        builder.Property(e => e.Description).HasColumnName("description");
        builder.Property(e => e.CreditHours).HasColumnName("credit_hours").IsRequired();
        builder.Property(e => e.LectureHours).HasColumnName("lecture_hours");
        builder.Property(e => e.LabHours).HasColumnName("lab_hours");
        builder.Property(e => e.MaxStudents).HasColumnName("max_students");
        builder.HasIndex(e => e.CourseCode).IsUnique();
        builder.HasMany(e => e.Prerequisites)
            .WithOne(p => p.Course)
            .HasForeignKey(p => p.CourseId)
            .OnDelete(DeleteBehavior.Cascade);
        builder.HasMany(e => e.DependentCourses)
            .WithOne(p => p.PrerequisiteCourse)
            .HasForeignKey(p => p.PrerequisiteCourseId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

===== Configurations/CourseEnrollmentConfiguration.cs =====
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using University.Core.Entities;
using University.Shared.Enums;

namespace University.Infrastructure.Data.Configurations;

public class CourseEnrollmentConfiguration : IEntityTypeConfiguration<CourseEnrollment>
{
    public void Configure(EntityTypeBuilder<CourseEnrollment> builder)
    {
        builder.ToTable("course_enrollments");
        builder.ConfigureBase();
        builder.Property(e => e.StudentId).HasColumnName("student_id");
        builder.Property(e => e.SectionId).HasColumnName("section_id");
        builder.Property(e => e.SemesterId).HasColumnName("semester_id");
        builder.Property(e => e.EnrollmentDate).HasColumnName("enrollment_date");
        builder.Property(e => e.Status).HasColumnName("status")
            .HasVarcharEnumConversion<EnrollmentStatus>()
            .HasMaxLength(20);
        builder.HasIndex(e => new { e.StudentId, e.SectionId, e.SemesterId }).IsUnique();
        builder.HasIndex(e => e.StudentId);
        builder.HasIndex(e => e.SectionId);
        builder.HasIndex(e => e.SemesterId);
        builder.HasOne(e => e.Student)
            .WithMany(s => s.Enrollments)
            .HasForeignKey(e => e.StudentId)
            .OnDelete(DeleteBehavior.Cascade);
        builder.HasOne(e => e.Section)
            .WithMany(s => s.Enrollments)
            .HasForeignKey(e => e.SectionId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(e => e.Semester)
            .WithMany(s => s.Enrollments)
            .HasForeignKey(e => e.SemesterId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

===== Configurations/CoursePrerequisiteConfiguration.cs =====
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using University.Core.Entities;

namespace University.Infrastructure.Data.Configurations;

public class CoursePrerequisiteConfiguration : IEntityTypeConfiguration<CoursePrerequisite>
{
    public void Configure(EntityTypeBuilder<CoursePrerequisite> builder)
    {
        builder.ToTable("course_prerequisites");
        builder.ConfigureBase();
        builder.Property(e => e.CourseId).HasColumnName("course_id");
        builder.Property(e => e.PrerequisiteCourseId).HasColumnName("prerequisite_course_id");
        builder.Property(e => e.IsMandatory).HasColumnName("is_mandatory");
        builder.HasIndex(e => new { e.CourseId, e.PrerequisiteCourseId }).IsUnique();
    }
}

===== Configurations/CourseSectionConfiguration.cs =====
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using University.Core.Entities;

namespace University.Infrastructure.Data.Configurations;

public class CourseSectionConfiguration : IEntityTypeConfiguration<CourseSection>
{
    public void Configure(EntityTypeBuilder<CourseSection> builder)
    {
        builder.ToTable("course_sections");
        builder.ConfigureBase();
        builder.Property(e => e.CourseId).HasColumnName("course_id");
        builder.Property(e => e.SemesterId).HasColumnName("semester_id");
        builder.Property(e => e.ClassroomId).HasColumnName("classroom_id");
        builder.Property(e => e.InstructorId).HasColumnName("instructor_id");
        builder.Property(e => e.SectionNumber).HasColumnName("section_number").HasMaxLength(10).IsRequired();
        builder.Property(e => e.MaxCapacity).HasColumnName("max_capacity");
        builder.Property(e => e.CurrentEnrollment).HasColumnName("current_enrollment");
        builder.Property(e => e.ScheduleDays).HasColumnName("schedule_days").HasMaxLength(20);
        builder.Property(e => e.StartTime).HasColumnName("start_time").HasColumnType("time");
        builder.Property(e => e.EndTime).HasColumnName("end_time").HasColumnType("time");
        builder.HasIndex(e => new { e.CourseId, e.SemesterId, e.SectionNumber }).IsUnique();
        builder.HasIndex(e => new { e.CourseId, e.SemesterId });
        builder.HasOne(e => e.Course)
            .WithMany(c => c.Sections)
            .HasForeignKey(e => e.CourseId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(e => e.Semester)
            .WithMany(s => s.Sections)
            .HasForeignKey(e => e.SemesterId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(e => e.Classroom)
            .WithMany(c => c.Sections)
            .HasForeignKey(e => e.ClassroomId)
            .OnDelete(DeleteBehavior.SetNull);
        builder.HasOne(e => e.Instructor)
            .WithMany(i => i.Sections)
            .HasForeignKey(e => e.InstructorId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}

===== Configurations/EmployeeConfiguration.cs =====
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using University.Core.Entities;

namespace University.Infrastructure.Data.Configurations;

public class EmployeeConfiguration : IEntityTypeConfiguration<Employee>
{
    public void Configure(EntityTypeBuilder<Employee> builder)
    {
        builder.ToTable("employees");
        builder.ConfigureBase();
        builder.Property(e => e.EmployeeNumber).HasColumnName("employee_number").HasMaxLength(50).IsRequired();
        builder.Property(e => e.FullName).HasColumnName("full_name").HasMaxLength(100).IsRequired();
        builder.Property(e => e.Email).HasColumnName("email").HasMaxLength(100).IsRequired();
        builder.Property(e => e.Phone).HasColumnName("phone").HasMaxLength(20);
        builder.Property(e => e.DepartmentId).HasColumnName("department_id");
        builder.Property(e => e.BranchId).HasColumnName("branch_id");
        builder.Property(e => e.ContractType).HasColumnName("contract_type").HasMaxLength(20);
        builder.Property(e => e.Status).HasColumnName("status").HasMaxLength(20);
        builder.Property(e => e.HireDate).HasColumnName("hire_date");
        builder.HasIndex(e => e.EmployeeNumber).IsUnique();
        builder.HasIndex(e => e.Email).IsUnique();

        builder.HasOne(e => e.Department)
            .WithMany()
            .HasForeignKey(e => e.DepartmentId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasOne(e => e.Branch)
            .WithMany()
            .HasForeignKey(e => e.BranchId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}
===== Configurations/FacultyConfiguration.cs =====
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using University.Core.Entities;

namespace University.Infrastructure.Data.Configurations;

public class FacultyConfiguration : IEntityTypeConfiguration<Faculty>
{
    public void Configure(EntityTypeBuilder<Faculty> builder)
    {
        builder.ToTable("faculties");
        builder.ConfigureBase();
        builder.Property(e => e.BranchId).HasColumnName("branch_id");
        builder.Property(e => e.FacultyName).HasColumnName("faculty_name").HasMaxLength(100).IsRequired();
        builder.Property(e => e.FacultyCode).HasColumnName("faculty_code").HasMaxLength(20);
        builder.Property(e => e.DeanName).HasColumnName("dean_name").HasMaxLength(100);
        builder.Property(e => e.Location).HasColumnName("location").HasMaxLength(200);
        builder.Property(e => e.Description).HasColumnName("description").HasMaxLength(500);
        builder.HasIndex(e => e.FacultyCode).IsUnique();
        builder.HasOne(e => e.Branch)
            .WithMany(b => b.Faculties)
            .HasForeignKey(e => e.BranchId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

===== Configurations/FinancialRecordConfiguration.cs =====
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using University.Core.Entities;
using University.Shared.Enums;

namespace University.Infrastructure.Data.Configurations;

public class FinancialRecordConfiguration : IEntityTypeConfiguration<FinancialRecord>
{
    public void Configure(EntityTypeBuilder<FinancialRecord> builder)
    {
        builder.ToTable("financial_records");
        builder.ConfigureBase();
        builder.Property(e => e.StudentId).HasColumnName("student_id");
        builder.Property(e => e.SemesterId).HasColumnName("semester_id");
        builder.Property(e => e.TotalDue).HasColumnName("total_due").HasPrecision(10, 2);
        builder.Property(e => e.TotalPaid).HasColumnName("total_paid").HasPrecision(10, 2);
        builder.Property(e => e.Balance).HasColumnName("balance").HasPrecision(10, 2);
        builder.Property(e => e.Status).HasColumnName("status")
            .HasVarcharEnumConversion<FinancialRecordStatus>()
            .HasMaxLength(20);
        builder.HasIndex(e => new { e.StudentId, e.SemesterId }).IsUnique();
        builder.HasOne(e => e.Student)
            .WithMany(s => s.FinancialRecords)
            .HasForeignKey(e => e.StudentId)
            .OnDelete(DeleteBehavior.Cascade);
        builder.HasOne(e => e.Semester)
            .WithMany(s => s.FinancialRecords)
            .HasForeignKey(e => e.SemesterId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}

===== Configurations/FinancialRefundConfiguration.cs =====
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using University.Core.Entities;
using University.Shared.Enums;

namespace University.Infrastructure.Data.Configurations;

public class FinancialRefundConfiguration : IEntityTypeConfiguration<FinancialRefund>
{
    public void Configure(EntityTypeBuilder<FinancialRefund> builder)
    {
        builder.ToTable("financial_refunds");
        builder.ConfigureBase();
        builder.Property(e => e.FinancialId).HasColumnName("financial_id");
        builder.Property(e => e.Amount).HasColumnName("amount").HasPrecision(10, 2).IsRequired();
        builder.Property(e => e.Reason).HasColumnName("reason").HasMaxLength(255).IsRequired();
        builder.Property(e => e.Status).HasColumnName("status")
            .HasVarcharEnumConversion<RefundStatus>()
            .HasMaxLength(20);
        builder.HasOne(e => e.FinancialRecord)
            .WithMany(f => f.Refunds)
            .HasForeignKey(e => e.FinancialId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

===== Configurations/GradeConfiguration.cs =====
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using University.Core.Entities;
using University.Shared.Enums;

namespace University.Infrastructure.Data.Configurations;

public class GradeConfiguration : IEntityTypeConfiguration<Grade>
{
    public void Configure(EntityTypeBuilder<Grade> builder)
    {
        builder.ToTable("grades");
        builder.ConfigureBase();
        builder.Property(e => e.EnrollmentId).HasColumnName("enrollment_id");
        builder.Property(e => e.MidtermScore).HasColumnName("midterm_score").HasPrecision(5, 2);
        builder.Property(e => e.FinalScore).HasColumnName("final_score").HasPrecision(5, 2);
        builder.Property(e => e.TotalScore).HasColumnName("total_score").HasPrecision(5, 2);
        builder.Property(e => e.LetterGrade).HasColumnName("letter_grade")
            .HasVarcharEnumConversion<GradeLetter>()
            .HasMaxLength(5);
        builder.Property(e => e.GradePoints).HasColumnName("grade_points").HasPrecision(3, 2);
        builder.Property(e => e.IsLocked).HasColumnName("is_locked");
        builder.HasIndex(e => e.EnrollmentId).IsUnique();
        builder.HasIndex(e => e.EnrollmentId).HasDatabaseName("idx_grades_enrollment");
        builder.HasOne(e => e.Enrollment)
            .WithOne(en => en.Grade)
            .HasForeignKey<Grade>(e => e.EnrollmentId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

===== Configurations/GraduationRequestConfiguration.cs =====
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using University.Core.Entities;
using University.Shared.Enums;

namespace University.Infrastructure.Data.Configurations;

public class GraduationRequestConfiguration : IEntityTypeConfiguration<GraduationRequest>
{
    public void Configure(EntityTypeBuilder<GraduationRequest> builder)
    {
        builder.ToTable("graduation_requests");
        builder.ConfigureBase();
        builder.Property(e => e.StudentId).HasColumnName("student_id");
        builder.Property(e => e.RequestDate).HasColumnName("request_date");
        builder.Property(e => e.ExpectedGraduationDate).HasColumnName("expected_graduation_date");
        builder.Property(e => e.Status).HasColumnName("status")
            .HasVarcharEnumConversion<GraduationStatus>()
            .HasMaxLength(20);
        builder.Property(e => e.ClearanceStatus).HasColumnName("clearance_status")
            .HasVarcharEnumConversion<ClearanceStatus>()
            .HasMaxLength(20);
        builder.Property(e => e.GpaAtRequest).HasColumnName("gpa_at_request").HasPrecision(4, 2);
        builder.Property(e => e.TotalCreditsAtRequest).HasColumnName("total_credits_at_request");
        builder.Property(e => e.Notes).HasColumnName("notes");
        builder.HasOne(e => e.Student)
            .WithMany(s => s.GraduationRequests)
            .HasForeignKey(e => e.StudentId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

===== Configurations/GroupConfiguration.cs =====
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using University.Core.Entities;

namespace University.Infrastructure.Data.Configurations;

public class GroupConfiguration : IEntityTypeConfiguration<Group>
{
    public void Configure(EntityTypeBuilder<Group> builder)
    {
        builder.ToTable("groups");
        builder.ConfigureBase();
        builder.Property(e => e.Name).HasColumnName("name").HasMaxLength(100).IsRequired();
        builder.Property(e => e.DisplayName).HasColumnName("display_name").HasMaxLength(200);
        builder.Property(e => e.Description).HasColumnName("description").HasMaxLength(500);
        builder.Property(e => e.BranchCode).HasColumnName("branch_code").HasMaxLength(50);
        builder.HasIndex(e => e.Name).IsUnique();
        builder.HasIndex(e => e.BranchCode).HasDatabaseName("idx_groups_branch_code");
    }
}

===== Configurations/GuardianConfiguration.cs =====
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using University.Core.Entities;

namespace University.Infrastructure.Data.Configurations;

public class GuardianConfiguration : IEntityTypeConfiguration<Guardian>
{
    public void Configure(EntityTypeBuilder<Guardian> builder)
    {
        builder.ToTable("guardians");
        builder.ConfigureBase();
        builder.Property(e => e.StudentId).HasColumnName("student_id");
        builder.Property(e => e.FullName).HasColumnName("full_name").HasMaxLength(100).IsRequired();
        builder.Property(e => e.Phone).HasColumnName("phone").HasMaxLength(20);
        builder.Property(e => e.Email).HasColumnName("email").HasMaxLength(100);
        builder.Property(e => e.Relationship).HasColumnName("relationship").HasMaxLength(50).IsRequired();
        builder.Property(e => e.IsPrimary).HasColumnName("is_primary");
        builder.HasOne(e => e.Student)
            .WithMany(s => s.Guardians)
            .HasForeignKey(e => e.StudentId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

===== Configurations/InstructorConfiguration.cs =====
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using University.Core.Entities;
using University.Shared.Enums;

namespace University.Infrastructure.Data.Configurations;

public class InstructorConfiguration : IEntityTypeConfiguration<Instructor>
{
    public void Configure(EntityTypeBuilder<Instructor> builder)
    {
        builder.ToTable("instructors");
        builder.ConfigureBase();
        builder.Property(e => e.FacultyId).HasColumnName("faculty_id");
        builder.Property(e => e.InstructorNumber).HasColumnName("instructor_number").HasMaxLength(20).IsRequired();
        builder.Property(e => e.AcademicRank).HasColumnName("academic_rank")
            .HasVarcharEnumConversion<AcademicRank>()
            .HasMaxLength(30);
        builder.Property(e => e.Specialization).HasColumnName("specialization").HasMaxLength(100);
        builder.HasIndex(e => e.InstructorNumber).IsUnique();
        // Shared-Primary-Key 1:1: instructors.id IS employees.id (instructors are employees).
        builder.HasOne(e => e.Employee)
            .WithOne(emp => emp.Instructor)
            .HasForeignKey<Instructor>(e => e.Id)
            .OnDelete(DeleteBehavior.Cascade);
        builder.HasOne(e => e.Faculty)
            .WithMany(f => f.Instructors)
            .HasForeignKey(e => e.FacultyId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}

===== Configurations/MajorConfiguration.cs =====
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using University.Core.Entities;

namespace University.Infrastructure.Data.Configurations;

public class MajorConfiguration : IEntityTypeConfiguration<Major>
{
    public void Configure(EntityTypeBuilder<Major> builder)
    {
        builder.ToTable("majors");
        builder.ConfigureBase();
        builder.Property(e => e.DepartmentId).HasColumnName("department_id");
        builder.Property(e => e.MajorName).HasColumnName("major_name").HasMaxLength(100).IsRequired();
        builder.Property(e => e.MajorCode).HasColumnName("major_code").HasMaxLength(20);
        builder.Property(e => e.TotalCreditHours).HasColumnName("total_credit_hours");
        builder.HasIndex(e => e.MajorCode).IsUnique();
        builder.HasOne(e => e.Department)
            .WithMany(d => d.Majors)
            .HasForeignKey(e => e.DepartmentId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

===== Configurations/ModuleConfiguration.cs =====
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using University.Core.Entities;

namespace University.Infrastructure.Data.Configurations;

public class ModuleConfiguration : IEntityTypeConfiguration<Module>
{
    public void Configure(EntityTypeBuilder<Module> builder)
    {
        builder.ToTable("modules");
        builder.ConfigureBase();
        builder.Property(e => e.Code).HasColumnName("code").HasMaxLength(100).IsRequired();
        builder.Property(e => e.Name).HasColumnName("name").HasMaxLength(200).IsRequired();
        builder.Property(e => e.Description).HasColumnName("description").HasMaxLength(500);
        builder.Property(e => e.BranchCode).HasColumnName("branch_code").HasMaxLength(50);
        builder.HasIndex(e => e.Code).IsUnique();
        builder.HasIndex(e => e.BranchCode).HasDatabaseName("idx_modules_branch_code");
        builder.HasMany(e => e.Permissions)
            .WithOne()
            .HasForeignKey(p => p.ModuleCode)
            .HasPrincipalKey(m => m.Code)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

===== Configurations/PaymentConfiguration.cs =====
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using University.Core.Entities;
using University.Shared.Enums;

namespace University.Infrastructure.Data.Configurations;

public class PaymentConfiguration : IEntityTypeConfiguration<Payment>
{
    public void Configure(EntityTypeBuilder<Payment> builder)
    {
        builder.ToTable("payments");
        builder.ConfigureBase();
        builder.Property(e => e.FinancialId).HasColumnName("financial_id");
        builder.Property(e => e.Amount).HasColumnName("amount").HasPrecision(10, 2).IsRequired();
        builder.Property(e => e.PaymentMethod).HasColumnName("payment_method")
            .HasVarcharEnumConversion<PaymentMethod>()
            .HasMaxLength(20)
            .IsRequired();
        builder.Property(e => e.PaymentDate).HasColumnName("payment_date");
        builder.Property(e => e.TransactionId).HasColumnName("transaction_id").HasMaxLength(100);
        builder.Property(e => e.Notes).HasColumnName("notes").HasMaxLength(255);
        builder.HasIndex(e => e.TransactionId).IsUnique();
        builder.HasIndex(e => e.FinancialId).HasDatabaseName("idx_payments_financial");
        builder.HasOne(e => e.FinancialRecord)
            .WithMany(f => f.Payments)
            .HasForeignKey(e => e.FinancialId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

===== Configurations/PermissionConfiguration.cs =====
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using University.Core.Entities;

namespace University.Infrastructure.Data.Configurations;

public class PermissionConfiguration : IEntityTypeConfiguration<Permission>
{
    public void Configure(EntityTypeBuilder<Permission> builder)
    {
        builder.ToTable("permissions");
        builder.ConfigureBase();
        builder.Property(e => e.PermissionName).HasColumnName("permission_name").HasMaxLength(100).IsRequired();
        builder.Property(e => e.Description).HasColumnName("description").HasMaxLength(255);
        builder.Property(e => e.Module).HasColumnName("module").HasMaxLength(50).IsRequired();
        builder.Property(e => e.ModuleCode).HasColumnName("module_code").HasMaxLength(100);
        builder.Property(e => e.IsSensitive).HasColumnName("is_sensitive").HasDefaultValue(false);
        builder.Property(e => e.BranchCode).HasColumnName("branch_code").HasMaxLength(50);
        builder.HasIndex(e => e.PermissionName).IsUnique();
        builder.HasIndex(e => e.ModuleCode).HasDatabaseName("idx_permissions_module_code");
    }
}

===== Configurations/RoleConfiguration.cs =====
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using University.Core.Entities;

namespace University.Infrastructure.Data.Configurations;

public class RoleConfiguration : IEntityTypeConfiguration<Role>
{
    public void Configure(EntityTypeBuilder<Role> builder)
    {
        builder.ToTable("roles");
        builder.ConfigureBase();
        builder.Property(e => e.RoleName).HasColumnName("role_name").HasMaxLength(50).IsRequired();
        builder.Property(e => e.Description).HasColumnName("description").HasMaxLength(255);
        builder.Property(e => e.DisplayName).HasColumnName("display_name").HasMaxLength(200);
        builder.Property(e => e.IsSystemRole).HasColumnName("is_system_role").HasDefaultValue(false);
        builder.Property(e => e.BranchCode).HasColumnName("branch_code").HasMaxLength(50);
        builder.HasIndex(e => e.RoleName).IsUnique();
        builder.HasIndex(e => e.BranchCode).HasDatabaseName("idx_roles_branch_code");
    }
}

===== Configurations/RolePermissionConfiguration.cs =====
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using University.Core.Entities;

namespace University.Infrastructure.Data.Configurations;

public class RolePermissionConfiguration : IEntityTypeConfiguration<RolePermission>
{
    public void Configure(EntityTypeBuilder<RolePermission> builder)
    {
        builder.ToTable("role_permissions");
        builder.ConfigureBase();
        builder.Property(e => e.RoleId).HasColumnName("role_id");
        builder.Property(e => e.PermissionId).HasColumnName("permission_id");
        builder.Property(e => e.GrantedBy).HasColumnName("granted_by");
        builder.Property(e => e.GrantedAt).HasColumnName("granted_at");
        builder.Property(e => e.BranchCode).HasColumnName("branch_code").HasMaxLength(50);
        builder.HasIndex(e => new { e.RoleId, e.PermissionId }).IsUnique();
        builder.HasIndex(e => e.BranchCode).HasDatabaseName("ix_role_permissions_branch_code");
        builder.HasOne(e => e.Role)
            .WithMany(r => r.RolePermissions)
            .HasForeignKey(e => e.RoleId)
            .OnDelete(DeleteBehavior.Cascade);
        builder.HasOne(e => e.Permission)
            .WithMany(p => p.RolePermissions)
            .HasForeignKey(e => e.PermissionId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

===== Configurations/ScholarshipConfiguration.cs =====
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using University.Core.Entities;

namespace University.Infrastructure.Data.Configurations;

public class ScholarshipConfiguration : IEntityTypeConfiguration<Scholarship>
{
    public void Configure(EntityTypeBuilder<Scholarship> builder)
    {
        builder.ToTable("scholarships");
        builder.ConfigureBase();
        builder.Property(e => e.ScholarshipName).HasColumnName("scholarship_name").HasMaxLength(100).IsRequired();
        builder.Property(e => e.ScholarshipCode).HasColumnName("scholarship_code").HasMaxLength(20).IsRequired();
        builder.Property(e => e.DiscountPercentage).HasColumnName("discount_percentage").HasPrecision(5, 2).IsRequired();
        builder.Property(e => e.MaxAmount).HasColumnName("max_amount").HasPrecision(10, 2);
        builder.HasIndex(e => e.ScholarshipCode).IsUnique();
    }
}

===== Configurations/SecuritySettingsOverrideConfiguration.cs =====
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using University.Core.Entities;

namespace University.Infrastructure.Data.Configurations;

public class SecuritySettingsOverrideConfiguration : IEntityTypeConfiguration<SecuritySettingsOverride>
{
    public void Configure(EntityTypeBuilder<SecuritySettingsOverride> builder)
    {
        builder.ToTable("security_settings_overrides");
        builder.ConfigureBase();
        builder.Property(e => e.Key).HasColumnName("key").HasMaxLength(100).IsRequired();
        builder.Property(e => e.ValueJson).HasColumnName("value_json").HasColumnType("jsonb");
        builder.Property(e => e.UpdatedAt).HasColumnName("updated_at");
        builder.HasIndex(e => e.Key).IsUnique();
    }
}

===== Configurations/SemesterConfiguration.cs =====
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using University.Core.Entities;
using University.Shared.Enums;

namespace University.Infrastructure.Data.Configurations;

public class SemesterConfiguration : IEntityTypeConfiguration<Semester>
{
    public void Configure(EntityTypeBuilder<Semester> builder)
    {
        builder.ToTable("semesters");
        builder.ConfigureBase();
        builder.Property(e => e.SemesterName).HasColumnName("semester_name").HasMaxLength(50).IsRequired();
        builder.Property(e => e.SemesterCode).HasColumnName("semester_code").HasMaxLength(20).IsRequired();
        builder.Property(e => e.AcademicYear).HasColumnName("academic_year").HasMaxLength(9).IsRequired();
        builder.Property(e => e.StartDate).HasColumnName("start_date").IsRequired();
        builder.Property(e => e.EndDate).HasColumnName("end_date").IsRequired();
        builder.Property(e => e.RegistrationStart).HasColumnName("registration_start");
        builder.Property(e => e.RegistrationEnd).HasColumnName("registration_end");
        builder.Property(e => e.Status).HasColumnName("status")
            .HasVarcharEnumConversion<SemesterStatus>()
            .HasMaxLength(20);
        builder.Property(e => e.IsCurrent).HasColumnName("is_current");
        builder.HasIndex(e => e.SemesterCode).IsUnique();
    }
}
===== Configurations/StudentConfiguration.cs =====
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using University.Core.Entities;
using University.Shared.Enums;

namespace University.Infrastructure.Data.Configurations;

public class StudentConfiguration : IEntityTypeConfiguration<Student>
{
    public void Configure(EntityTypeBuilder<Student> builder)
    {
        builder.ToTable("students");
        builder.ConfigureBase();
        builder.Property(e => e.MajorId).HasColumnName("major_id");
        builder.Property(e => e.StudentNumber).HasColumnName("student_number").HasMaxLength(20).IsRequired();
        builder.Property(e => e.Gpa).HasColumnName("gpa").HasPrecision(4, 2);
        builder.Property(e => e.CompletedCredits).HasColumnName("completed_credits");
        builder.Property(e => e.EnrollmentDate).HasColumnName("enrollment_date");
        builder.Property(e => e.Status).HasColumnName("status")
            .HasVarcharEnumConversion<StudentStatus>()
            .HasMaxLength(20);
        builder.HasIndex(e => e.StudentNumber).IsUnique();

        // User link: Student.UserId -> users.id. Deleting the User keeps the Student with
        // UserId = NULL (SetNull). NOT a shared primary key (student.Id != user.Id).
        builder.HasOne(e => e.User)
            .WithMany()
            .HasForeignKey(e => e.UserId)
            .OnDelete(DeleteBehavior.SetNull);
        builder.Property(e => e.UserId).IsRequired(false);

        builder.HasOne(e => e.Major)
            .WithMany(m => m.Students)
            .HasForeignKey(e => e.MajorId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}

===== Configurations/StudyPlanConfiguration.cs =====
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using University.Core.Entities;

namespace University.Infrastructure.Data.Configurations;

public class StudyPlanConfiguration : IEntityTypeConfiguration<StudyPlan>
{
    public void Configure(EntityTypeBuilder<StudyPlan> builder)
    {
        builder.ToTable("study_plans");
        builder.ConfigureBase();
        builder.Property(e => e.MajorId).HasColumnName("major_id");
        builder.Property(e => e.CourseId).HasColumnName("course_id");
        builder.Property(e => e.SemesterNumber).HasColumnName("semester_number");
        builder.Property(e => e.IsMandatory).HasColumnName("is_mandatory");
        builder.HasIndex(e => new { e.MajorId, e.CourseId }).IsUnique();
        builder.HasOne(e => e.Major)
            .WithMany()
            .HasForeignKey(e => e.MajorId)
            .OnDelete(DeleteBehavior.Cascade);
        builder.HasOne(e => e.Course)
            .WithMany(c => c.StudyPlans)
            .HasForeignKey(e => e.CourseId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

===== Configurations/SystemNotificationConfiguration.cs =====
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using University.Core.Entities;
using University.Shared.Enums;

namespace University.Infrastructure.Data.Configurations;

public class SystemNotificationConfiguration : IEntityTypeConfiguration<SystemNotification>
{
    public void Configure(EntityTypeBuilder<SystemNotification> builder)
    {
        builder.ToTable("system_notifications");
        builder.ConfigureBase();
        builder.Property(e => e.UserId).HasColumnName("user_id");
        builder.Property(e => e.Title).HasColumnName("title").HasMaxLength(150).IsRequired();
        builder.Property(e => e.Message).HasColumnName("message").IsRequired();
        builder.Property(e => e.NotificationType).HasColumnName("notification_type")
            .HasVarcharEnumConversion<NotificationType>()
            .HasMaxLength(30);
        builder.Property(e => e.IsRead).HasColumnName("is_read");
        builder.HasOne(e => e.User)
            .WithMany()
            .HasForeignKey(e => e.UserId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

===== Configurations/TuitionFeeConfiguration.cs =====
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using University.Core.Entities;

namespace University.Infrastructure.Data.Configurations;

public class TuitionFeeConfiguration : IEntityTypeConfiguration<TuitionFee>
{
    public void Configure(EntityTypeBuilder<TuitionFee> builder)
    {
        builder.ToTable("tuition_fees");
        builder.ConfigureBase();
        builder.Property(e => e.MajorId).HasColumnName("major_id");
        builder.Property(e => e.AcademicYear).HasColumnName("academic_year").HasMaxLength(9).IsRequired();
        builder.Property(e => e.CreditHourPrice).HasColumnName("credit_hour_price").HasPrecision(10, 2).IsRequired();
        builder.HasIndex(e => new { e.MajorId, e.AcademicYear }).IsUnique();
        builder.HasOne(e => e.Major)
            .WithMany()
            .HasForeignKey(e => e.MajorId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

===== Configurations/UserConfiguration.cs =====
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using University.Core.Entities;

namespace University.Infrastructure.Data.Configurations;

public class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> builder)
    {
        builder.ToTable("users");
        builder.ConfigureBase();
        // Id is the shared primary key: users.id == employees.id (or students.id).
        builder.Property(e => e.Id).ValueGeneratedNever();
        builder.Property(e => e.Username).HasColumnName("username").HasMaxLength(50).IsRequired();
        builder.Property(e => e.PasswordHash).HasColumnName("password_hash").HasMaxLength(255).IsRequired();
        builder.Property(e => e.RoleId).HasColumnName("role_id");
        builder.Property(e => e.MustChangePassword).HasColumnName("must_change_password").HasDefaultValue(true);
        builder.HasIndex(e => e.Username).IsUnique();
        builder.HasIndex(e => e.RoleId);

        // Shared-Primary-Key 1:1: users.id == employees.id.
        // Employee is principal; deleting an Employee deletes this User (Cascade), but deleting
        // this User does NOT delete the Employee.
        builder.HasOne(u => u.Employee)
            .WithOne(e => e.User)
            .HasForeignKey<User>(u => u.Id)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(e => e.Role)
            .WithMany(r => r.Users)
            .HasForeignKey(e => e.RoleId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

===== Configurations/UserGroupConfiguration.cs =====
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using University.Core.Entities;

namespace University.Infrastructure.Data.Configurations;

public class UserGroupConfiguration : IEntityTypeConfiguration<UserGroup>
{
    public void Configure(EntityTypeBuilder<UserGroup> builder)
    {
        builder.ToTable("user_groups");
        builder.ConfigureBase();
        builder.Property(e => e.UserId).HasColumnName("user_id");
        builder.Property(e => e.GroupId).HasColumnName("group_id");
        builder.Property(e => e.AssignedAt).HasColumnName("assigned_at");
        builder.Property(e => e.BranchCode).HasColumnName("branch_code").HasMaxLength(50);
        builder.HasIndex(e => new { e.UserId, e.GroupId }).IsUnique();
        builder.HasIndex(e => e.BranchCode).HasDatabaseName("idx_user_groups_branch_code");
        builder.HasOne(e => e.User)
            .WithMany(u => u.UserGroups)
            .HasForeignKey(e => e.UserId)
            .OnDelete(DeleteBehavior.Cascade);
        builder.HasOne(e => e.Group)
            .WithMany(g => g.UserGroups)
            .HasForeignKey(e => e.GroupId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

===== Configurations/UserRoleConfiguration.cs =====
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using University.Core.Entities;

namespace University.Infrastructure.Data.Configurations;

public class UserRoleConfiguration : IEntityTypeConfiguration<UserRole>
{
    public void Configure(EntityTypeBuilder<UserRole> builder)
    {
        builder.ToTable("user_roles");
        builder.ConfigureBase();
        builder.Property(e => e.UserId).HasColumnName("user_id");
        builder.Property(e => e.RoleId).HasColumnName("role_id");
        builder.Property(e => e.AssignedBy).HasColumnName("assigned_by");
        builder.Property(e => e.AssignedAt).HasColumnName("assigned_at");
        builder.Property(e => e.BranchCode).HasColumnName("branch_code").HasMaxLength(50);
        builder.HasIndex(e => new { e.UserId, e.RoleId }).IsUnique();
        builder.HasIndex(e => e.BranchCode).HasDatabaseName("idx_user_roles_branch_code");
        builder.HasOne(e => e.User)
            .WithMany(u => u.UserRoles)
            .HasForeignKey(e => e.UserId)
            .OnDelete(DeleteBehavior.Cascade);
        builder.HasOne(e => e.Role)
            .WithMany(r => r.UserRoles)
            .HasForeignKey(e => e.RoleId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
