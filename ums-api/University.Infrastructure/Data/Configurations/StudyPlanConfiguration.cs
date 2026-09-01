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
