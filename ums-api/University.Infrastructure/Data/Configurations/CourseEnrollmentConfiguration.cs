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
