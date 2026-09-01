using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using University.Shared.Common;

namespace University.Infrastructure.Data.Configurations;

public static class BaseEntityConfiguration
{
    public static void ConfigureBase<T>(this EntityTypeBuilder<T> builder) where T : BaseEntity
    {
        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id).HasColumnName("id");
        builder.Property(e => e.CreatedAt).HasColumnName("created_at");
        builder.Property(e => e.UpdatedAt).HasColumnName("updated_at");
        builder.Property(e => e.IsActive).HasColumnName("is_active");
    }

    public static PropertyBuilder<TEnum> HasVarcharEnumConversion<TEnum>(
        this PropertyBuilder<TEnum> builder) where TEnum : struct, Enum
    {
        return builder.HasConversion(
            v => v.ToString(),
            v => (TEnum)Enum.Parse(typeof(TEnum), v));
    }

    public static PropertyBuilder<TEnum?> HasVarcharEnumConversion<TEnum>(
        this PropertyBuilder<TEnum?> builder) where TEnum : struct, Enum
    {
        return builder.HasConversion(
            v => v.HasValue ? v.Value.ToString() : null,
            v => v == null ? (TEnum?)null : (TEnum)Enum.Parse(typeof(TEnum), v));
    }
}
