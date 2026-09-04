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
