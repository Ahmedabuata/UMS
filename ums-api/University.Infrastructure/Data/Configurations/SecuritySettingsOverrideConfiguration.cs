using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using University.Core.Entities;

namespace University.Infrastructure.Data.Configurations;

public class SecuritySettingsOverrideConfiguration : IEntityTypeConfiguration<SecuritySettingsOverride>
{
    public void Configure(EntityTypeBuilder<SecuritySettingsOverride> builder)
    {
        builder.ToTable("security_settings_overrides");
        builder.ConfigureBase();
        builder.Property(e => e.Key).HasColumnName("key").HasMaxLength(100).IsRequired();
        builder.Property(e => e.ValueJson).HasColumnName("value_json").HasColumnType("jsonb");
        builder.Property(e => e.UpdatedAt).HasColumnName("updated_at");
        builder.HasIndex(e => e.Key).IsUnique();
    }
}
