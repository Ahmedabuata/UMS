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
