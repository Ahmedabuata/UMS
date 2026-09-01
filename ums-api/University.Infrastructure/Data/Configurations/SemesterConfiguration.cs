using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using University.Core.Entities;

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
        builder.Property(e => e.IsCurrent).HasColumnName("is_current");
        builder.HasIndex(e => e.SemesterCode).IsUnique();
    }
}
