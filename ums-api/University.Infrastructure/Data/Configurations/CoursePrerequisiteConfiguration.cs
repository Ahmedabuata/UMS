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
