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
