using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using University.Core.Entities;

namespace University.Infrastructure.Data.Configurations;

public class UserRoleConfiguration : IEntityTypeConfiguration<UserRole>
{
    public void Configure(EntityTypeBuilder<UserRole> builder)
    {
        builder.ToTable("user_roles");
        builder.ConfigureBase();
        builder.Property(e => e.UserId).HasColumnName("user_id");
        builder.Property(e => e.RoleId).HasColumnName("role_id");
        builder.Property(e => e.AssignedBy).HasColumnName("assigned_by");
        builder.Property(e => e.AssignedAt).HasColumnName("assigned_at");
        builder.Property(e => e.BranchCode).HasColumnName("branch_code").HasMaxLength(50);
        builder.HasIndex(e => new { e.UserId, e.RoleId }).IsUnique();
        builder.HasIndex(e => e.BranchCode).HasDatabaseName("idx_user_roles_branch_code");
        builder.HasOne(e => e.User)
            .WithMany(u => u.UserRoles)
            .HasForeignKey(e => e.UserId)
            .OnDelete(DeleteBehavior.Cascade);
        builder.HasOne(e => e.Role)
            .WithMany(r => r.UserRoles)
            .HasForeignKey(e => e.RoleId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
