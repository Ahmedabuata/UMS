using Identity.Api.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Identity.Api.Configurations;

/// <summary>
/// EF Core configuration for the AuditArchiveJob entity.
/// 
/// Automatically applied via ApplyConfigurationsFromAssembly() 
/// in IdentityDbContext.OnModelCreating().
/// 
/// This configuration uses DEFENSE IN DEPTH principle:
/// - Data Annotations in the Entity ([MaxLength], [Column])
/// - Fluent API in this Configuration (HasMaxLength, HasColumnName)
/// 
/// CRITICAL: The HasConversion<string>() is MANDATORY here because
/// the Status enum MUST be stored as a string (varchar(50)) in PostgreSQL,
/// not as an integer (0, 1, 2).
/// </summary>
public class AuditArchiveJobConfiguration : IEntityTypeConfiguration<AuditArchiveJob>
{
    public void Configure(EntityTypeBuilder<AuditArchiveJob> builder)
    {
        // ============================================================
        // Table Mapping
        // ============================================================
        builder.ToTable("audit_archive_jobs");
        builder.HasKey(e => e.Id);

        // ============================================================
        // Property Mappings (Defense in Depth)
        // ============================================================
        // Explicitly define column names and lengths in the Configuration
        // even if they're already in the Entity as Data Annotations.
        // This prevents accidental schema changes.
        // ============================================================

        builder.Property(e => e.Id)
            .HasColumnName("id")
            .IsRequired();

        builder.Property(e => e.BatchId)
            .HasColumnName("batch_id")
            .IsRequired();

        // ============================================================
        // CRITICAL: Enum → String Conversion
        // ============================================================
        // Without this, EF Core saves enum as int (0,1,2), which 
        // conflicts with the varchar(50) column type in PostgreSQL.
        // This ensures "RUNNING", "COMPLETED", "FAILED" are saved as text.
        // 
        // The column type is explicitly set to varchar(50) to match
        // the database schema exactly.
        // ============================================================
        builder.Property(e => e.Status)
            .HasColumnName("status")
            .HasConversion<string>()
            .HasColumnType("varchar(50)")
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(e => e.RecordsCount)
            .HasColumnName("records_count")
            .IsRequired();

        builder.Property(e => e.EncryptionKeyVersion)
            .HasColumnName("encryption_key_version");

        builder.Property(e => e.ErrorMessage)
            .HasColumnName("error_message");

        builder.Property(e => e.StartedAt)
            .HasColumnName("started_at");

        builder.Property(e => e.CompletedAt)
            .HasColumnName("completed_at");

        builder.Property(e => e.TriggeredBy)
            .HasColumnName("triggered_by");

        // ============================================================
        // Unique Constraint: BatchId (Idempotency Guarantee)
        // ============================================================
        // Prevents creating two jobs with the same batch_id.
        // This is CRITICAL for idempotency: if the archive process
        // runs twice, the second run will fail due to this constraint.
        builder.HasIndex(e => e.BatchId)
            .IsUnique()
            .HasDatabaseName("uk_audit_archive_batch_id");

        // ============================================================
        // Index 1: Filter by Status (for monitoring dashboard)
        // ============================================================
        // Common query: "Show me all failed archive jobs"
        builder.HasIndex(e => e.Status)
            .HasDatabaseName("idx_audit_archive_jobs_status");

        // ============================================================
        // Index 2: Latest Jobs First (for dashboard sorting)
        // ============================================================
        // Common query: "Show me the latest 10 archive jobs"
        builder.HasIndex(e => e.StartedAt)
            .HasDatabaseName("idx_audit_archive_jobs_started_at");
    }
}