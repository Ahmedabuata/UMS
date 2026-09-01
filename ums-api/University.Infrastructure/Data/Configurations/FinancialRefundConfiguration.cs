using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using University.Core.Entities;
using University.Shared.Enums;

namespace University.Infrastructure.Data.Configurations;

public class FinancialRefundConfiguration : IEntityTypeConfiguration<FinancialRefund>
{
    public void Configure(EntityTypeBuilder<FinancialRefund> builder)
    {
        builder.ToTable("financial_refunds");
        builder.ConfigureBase();
        builder.Property(e => e.FinancialId).HasColumnName("financial_id");
        builder.Property(e => e.Amount).HasColumnName("amount").HasPrecision(10, 2).IsRequired();
        builder.Property(e => e.Reason).HasColumnName("reason").HasMaxLength(255).IsRequired();
        builder.Property(e => e.Status).HasColumnName("status")
            .HasVarcharEnumConversion<RefundStatus>()
            .HasMaxLength(20);
        builder.HasOne(e => e.FinancialRecord)
            .WithMany(f => f.Refunds)
            .HasForeignKey(e => e.FinancialId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
