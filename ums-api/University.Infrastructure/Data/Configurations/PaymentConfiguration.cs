using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using University.Core.Entities;
using University.Shared.Enums;

namespace University.Infrastructure.Data.Configurations;

public class PaymentConfiguration : IEntityTypeConfiguration<Payment>
{
    public void Configure(EntityTypeBuilder<Payment> builder)
    {
        builder.ToTable("payments");
        builder.ConfigureBase();
        builder.Property(e => e.FinancialId).HasColumnName("financial_id");
        builder.Property(e => e.Amount).HasColumnName("amount").HasPrecision(10, 2).IsRequired();
        builder.Property(e => e.PaymentMethod).HasColumnName("payment_method")
            .HasVarcharEnumConversion<PaymentMethod>()
            .HasMaxLength(20)
            .IsRequired();
        builder.Property(e => e.PaymentDate).HasColumnName("payment_date");
        builder.Property(e => e.TransactionId).HasColumnName("transaction_id").HasMaxLength(100);
        builder.Property(e => e.Notes).HasColumnName("notes").HasMaxLength(255);
        builder.HasIndex(e => e.TransactionId).IsUnique();
        builder.HasIndex(e => e.FinancialId).HasDatabaseName("idx_payments_financial");
        builder.HasOne(e => e.FinancialRecord)
            .WithMany(f => f.Payments)
            .HasForeignKey(e => e.FinancialId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
