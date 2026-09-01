using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using University.Core.Entities;

namespace University.Infrastructure.Data.Configurations;

public class TuitionFeeConfiguration : IEntityTypeConfiguration<TuitionFee>
{
    public void Configure(EntityTypeBuilder<TuitionFee> builder)
    {
        builder.ToTable("tuition_fees");
        builder.ConfigureBase();
        builder.Property(e => e.MajorId).HasColumnName("major_id");
        builder.Property(e => e.AcademicYear).HasColumnName("academic_year").HasMaxLength(9).IsRequired();
        builder.Property(e => e.CreditHourPrice).HasColumnName("credit_hour_price").HasPrecision(10, 2).IsRequired();
        builder.HasIndex(e => new { e.MajorId, e.AcademicYear }).IsUnique();
        builder.HasOne(e => e.Major)
            .WithMany()
            .HasForeignKey(e => e.MajorId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
