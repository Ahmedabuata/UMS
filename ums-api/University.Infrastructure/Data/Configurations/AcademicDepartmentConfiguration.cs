using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using University.Core.Entities;

namespace University.Infrastructure.Data.Configurations;

public class AcademicDepartmentConfiguration : IEntityTypeConfiguration<AcademicDepartment>
{
    public void Configure(EntityTypeBuilder<AcademicDepartment> builder)
    {
        builder.ToTable("academic_departments");
        builder.ConfigureBase();
        builder.Property(e => e.FacultyId).HasColumnName("faculty_id");
        builder.Property(e => e.DepartmentName).HasColumnName("department_name").HasMaxLength(100).IsRequired();
        builder.Property(e => e.DepartmentCode).HasColumnName("department_code").HasMaxLength(20);
        builder.Property(e => e.HeadName).HasColumnName("head_name").HasMaxLength(100);
        builder.HasIndex(e => e.DepartmentCode).IsUnique();
        builder.HasOne(e => e.Faculty)
            .WithMany(f => f.AcademicDepartments)
            .HasForeignKey(e => e.FacultyId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
