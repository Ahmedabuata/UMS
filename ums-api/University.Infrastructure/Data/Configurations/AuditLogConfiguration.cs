using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using University.Core.Entities;

namespace University.Infrastructure.Data.Configurations;

public class AuditLogConfiguration : IEntityTypeConfiguration<AuditLog>
{
    public void Configure(EntityTypeBuilder<AuditLog> builder)
    {
        builder.ToTable("audit_logs");
        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id).HasColumnName("id");
        builder.Property(e => e.UserId).HasColumnName("user_id");
        builder.Property(e => e.Action).HasColumnName("action").HasMaxLength(50).IsRequired();
        builder.Property(e => e.Entity).HasColumnName("entity").HasMaxLength(100);
        builder.Property(e => e.EntityId).HasColumnName("entity_id").HasMaxLength(50);
        builder.Property(e => e.TableName).HasColumnName("table_name").HasMaxLength(50);
        builder.Property(e => e.OldValues).HasColumnName("old_values").HasColumnType("jsonb");
        builder.Property(e => e.NewValues).HasColumnName("new_values").HasColumnType("jsonb");
        builder.Property(e => e.IpAddress).HasColumnName("ip_address").HasMaxLength(45);
        builder.Property(e => e.UserAgent).HasColumnName("user_agent").HasMaxLength(255);
        builder.Property(e => e.BranchCode).HasColumnName("branch_code").HasMaxLength(50);
        builder.Property(e => e.CreatedBy).HasColumnName("created_by");
        builder.Property(e => e.Timestamp).HasColumnName("timestamp");
        builder.Property(e => e.CreatedAt).HasColumnName("created_at");
        builder.HasIndex(e => e.UserId).HasDatabaseName("idx_audit_logs_user");
        builder.HasIndex(e => e.Timestamp).HasDatabaseName("idx_audit_logs_timestamp");
        builder.HasIndex(e => e.BranchCode).HasDatabaseName("idx_audit_logs_branch_code");
        builder.HasOne(e => e.User)
            .WithMany()
            .HasForeignKey(e => e.UserId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}
