using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using University.Core.Entities;

namespace University.Infrastructure.Data.Configurations;

public class AdministrativeDepartmentConfiguration : IEntityTypeConfiguration<AdministrativeDepartment>
{
    public void Configure(EntityTypeBuilder<AdministrativeDepartment> builder)
    {
        builder.ToTable("administrative_departments");
        builder.ConfigureBase();
        builder.Property(e => e.BranchId).HasColumnName("branch_id");
        builder.Property(e => e.DepartmentName).HasColumnName("department_name").HasMaxLength(100).IsRequired();
        builder.Property(e => e.DepartmentCode).HasColumnName("department_code").HasMaxLength(20).IsRequired();
        builder.Property(e => e.Description).HasColumnName("description").HasMaxLength(255);
        builder.HasIndex(e => e.DepartmentCode).IsUnique();
        builder.HasOne(e => e.Branch)
            .WithMany(b => b.AdministrativeDepartments)
            .HasForeignKey(e => e.BranchId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
