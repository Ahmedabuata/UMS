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
        builder.HasIndex(e => e.PermissionName).IsUnique();
    }
}
