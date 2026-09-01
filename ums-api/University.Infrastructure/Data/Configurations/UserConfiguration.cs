using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using University.Core.Entities;

namespace University.Infrastructure.Data.Configurations;

public class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> builder)
    {
        builder.ToTable("users");
        builder.ConfigureBase();
        builder.Property(e => e.Username).HasColumnName("username").HasMaxLength(50).IsRequired();
        builder.Property(e => e.Email).HasColumnName("email").HasMaxLength(100).IsRequired();
        builder.Property(e => e.PasswordHash).HasColumnName("password_hash").HasMaxLength(255).IsRequired();
        builder.Property(e => e.FullName).HasColumnName("full_name").HasMaxLength(100).IsRequired();
        builder.Property(e => e.PhoneNumber).HasColumnName("phone_number").HasMaxLength(20);
        builder.Property(e => e.BranchId).HasColumnName("branch_id");
        builder.Property(e => e.RoleId).HasColumnName("role_id");
        builder.Property(e => e.LastLogin).HasColumnName("last_login");
        builder.HasIndex(e => e.Username).IsUnique();
        builder.HasIndex(e => e.Email).IsUnique();
        builder.HasIndex(e => e.RoleId);
        builder.HasOne(e => e.Branch)
            .WithMany(b => b.Users)
            .HasForeignKey(e => e.BranchId)
            .OnDelete(DeleteBehavior.SetNull);
        builder.HasOne(e => e.Role)
            .WithMany(r => r.Users)
            .HasForeignKey(e => e.RoleId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
