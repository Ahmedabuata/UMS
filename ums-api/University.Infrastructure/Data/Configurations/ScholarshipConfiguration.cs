using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using University.Core.Entities;

namespace University.Infrastructure.Data.Configurations;

public class ScholarshipConfiguration : IEntityTypeConfiguration<Scholarship>
{
    public void Configure(EntityTypeBuilder<Scholarship> builder)
    {
        builder.ToTable("scholarships");
        builder.ConfigureBase();
        builder.Property(e => e.ScholarshipName).HasColumnName("scholarship_name").HasMaxLength(100).IsRequired();
        builder.Property(e => e.ScholarshipCode).HasColumnName("scholarship_code").HasMaxLength(20).IsRequired();
        builder.Property(e => e.DiscountPercentage).HasColumnName("discount_percentage").HasPrecision(5, 2).IsRequired();
        builder.Property(e => e.MaxAmount).HasColumnName("max_amount").HasPrecision(10, 2);
        builder.HasIndex(e => e.ScholarshipCode).IsUnique();
    }
}
