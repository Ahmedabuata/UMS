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
