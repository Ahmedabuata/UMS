using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using University.Core.Entities;

namespace University.Infrastructure.Data.Configurations;

public class RolePermissionConfiguration : IEntityTypeConfiguration<RolePermission>
{
    public void Configure(EntityTypeBuilder<RolePermission> builder)
    {
        builder.ToTable("role_permissions");
        builder.ConfigureBase();
        builder.Property(e => e.RoleId).HasColumnName("role_id");
        builder.Property(e => e.PermissionId).HasColumnName("permission_id");
        builder.Property(e => e.GrantedBy).HasColumnName("granted_by");
        builder.Property(e => e.GrantedAt).HasColumnName("granted_at");
        builder.Property(e => e.BranchCode).HasColumnName("branch_code").HasMaxLength(50);
        builder.HasIndex(e => new { e.RoleId, e.PermissionId }).IsUnique();
        builder.HasIndex(e => e.BranchCode).HasDatabaseName("ix_role_permissions_branch_code");
        builder.HasOne(e => e.Role)
            .WithMany(r => r.RolePermissions)
            .HasForeignKey(e => e.RoleId)
            .OnDelete(DeleteBehavior.Cascade);
        builder.HasOne(e => e.Permission)
            .WithMany(p => p.RolePermissions)
            .HasForeignKey(e => e.PermissionId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
