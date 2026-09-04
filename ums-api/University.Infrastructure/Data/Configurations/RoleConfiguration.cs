using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using University.Core.Entities;

namespace University.Infrastructure.Data.Configurations;

public class RoleConfiguration : IEntityTypeConfiguration<Role>
{
    public void Configure(EntityTypeBuilder<Role> builder)
    {
        builder.ToTable("roles");
        builder.ConfigureBase();
        builder.Property(e => e.RoleName).HasColumnName("role_name").HasMaxLength(50).IsRequired();
        builder.Property(e => e.Description).HasColumnName("description").HasMaxLength(255);
        builder.Property(e => e.DisplayName).HasColumnName("display_name").HasMaxLength(200);
        builder.Property(e => e.IsSystemRole).HasColumnName("is_system_role").HasDefaultValue(false);
        builder.Property(e => e.BranchCode).HasColumnName("branch_code").HasMaxLength(50);
        builder.HasIndex(e => e.RoleName).IsUnique();
        builder.HasIndex(e => e.BranchCode).HasDatabaseName("idx_roles_branch_code");
    }
}
