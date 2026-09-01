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
