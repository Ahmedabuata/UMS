using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using University.Core.Entities;

namespace University.Infrastructure.Data.Configurations;

public class PermissionConfiguration : IEntityTypeConfiguration<Permission>
{
    public void Configure(EntityTypeBuilder<Permission> builder)
    {
        builder.ToTable("permissions");
        builder.ConfigureBase();
        builder.Property(e => e.PermissionName).HasColumnName("permission_name").HasMaxLength(100).IsRequired();
        builder.Property(e => e.Description).HasColumnName("description").HasMaxLength(255);
        builder.Property(e => e.Module).HasColumnName("module").HasMaxLength(50).IsRequired();
        builder.Property(e => e.ModuleCode).HasColumnName("module_code").HasMaxLength(100);
        builder.Property(e => e.IsSensitive).HasColumnName("is_sensitive").HasDefaultValue(false);
        builder.Property(e => e.BranchCode).HasColumnName("branch_code").HasMaxLength(50);
        builder.HasIndex(e => e.PermissionName).IsUnique();
        builder.HasIndex(e => e.ModuleCode).HasDatabaseName("idx_permissions_module_code");
    }
}
