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
        builder.Property(e => e.UserId).HasColumnName("user_id");
        builder.Property(e => e.FacultyId).HasColumnName("faculty_id");
        builder.Property(e => e.InstructorNumber).HasColumnName("instructor_number").HasMaxLength(20).IsRequired();
        builder.Property(e => e.AcademicRank).HasColumnName("academic_rank")
            .HasVarcharEnumConversion<AcademicRank>()
            .HasMaxLength(30);
        builder.Property(e => e.Specialization).HasColumnName("specialization").HasMaxLength(100);
        builder.HasIndex(e => e.UserId).IsUnique();
        builder.HasIndex(e => e.InstructorNumber).IsUnique();
        builder.HasOne(e => e.User)
            .WithOne(u => u.Instructor)
            .HasForeignKey<Instructor>(e => e.UserId)
            .OnDelete(DeleteBehavior.Cascade);
        builder.HasOne(e => e.Faculty)
            .WithMany(f => f.Instructors)
            .HasForeignKey(e => e.FacultyId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}
