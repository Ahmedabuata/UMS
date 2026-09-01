using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using University.Core.Entities;
using University.Shared.Enums;

namespace University.Infrastructure.Data.Configurations;

public class FinancialRecordConfiguration : IEntityTypeConfiguration<FinancialRecord>
{
    public void Configure(EntityTypeBuilder<FinancialRecord> builder)
    {
        builder.ToTable("financial_records");
        builder.ConfigureBase();
        builder.Property(e => e.StudentId).HasColumnName("student_id");
        builder.Property(e => e.SemesterId).HasColumnName("semester_id");
        builder.Property(e => e.TotalDue).HasColumnName("total_due").HasPrecision(10, 2);
        builder.Property(e => e.TotalPaid).HasColumnName("total_paid").HasPrecision(10, 2);
        builder.Property(e => e.Balance).HasColumnName("balance").HasPrecision(10, 2);
        builder.Property(e => e.Status).HasColumnName("status")
            .HasVarcharEnumConversion<FinancialRecordStatus>()
            .HasMaxLength(20);
        builder.HasIndex(e => new { e.StudentId, e.SemesterId }).IsUnique();
        builder.HasOne(e => e.Student)
            .WithMany(s => s.FinancialRecords)
            .HasForeignKey(e => e.StudentId)
            .OnDelete(DeleteBehavior.Cascade);
        builder.HasOne(e => e.Semester)
            .WithMany(s => s.FinancialRecords)
            .HasForeignKey(e => e.SemesterId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}
