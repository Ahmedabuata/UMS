using Identity.Api.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Identity.Api.Configurations;

/// <summary>
/// EF Core configuration for the AuditLogArchive entity.
/// 
/// Automatically applied via ApplyConfigurationsFromAssembly() 
/// in IdentityDbContext.OnModelCreating().
/// 
/// Responsibilities:
/// - Table mapping: audit_logs_archive
/// - Primary key: Id
/// - Indexes: 6 (for historical queries)
/// 
/// NOTE: No Foreign Keys (by design) for performance and deletion safety.
/// </summary>
public class AuditLogArchiveConfiguration : IEntityTypeConfiguration<AuditLogArchive>
{
    public void Configure(EntityTypeBuilder<AuditLogArchive> builder)
    {
        // ============================================================
        // Table Mapping
        // ============================================================
        builder.ToTable("audit_logs_archive");
        builder.HasKey(e => e.Id);

        // ============================================================
        // Index 1: Filter by timestamp (most common query)
        // ============================================================
        builder.HasIndex(e => e.Timestamp)
            .HasDatabaseName("idx_audit_archive_timestamp");

        // ============================================================
        // Index 2: Filter by user
        // ============================================================
        builder.HasIndex(e => e.UserId)
            .HasDatabaseName("idx_audit_archive_user_id");

        // ============================================================
        // Index 3: Link records to their batch
        // ============================================================
        builder.HasIndex(e => e.ArchiveBatchId)
            .HasDatabaseName("idx_audit_archive_batch_id");

        // ============================================================
        // Index 4: Latest archived records first
        // ============================================================
        builder.HasIndex(e => e.ArchivedAt)
            .HasDatabaseName("idx_audit_archive_archived_at");

        // ============================================================
        // Index 5: Lookup by entity
        // ============================================================
        builder.HasIndex(e => new { e.Entity, e.EntityId })
            .HasDatabaseName("idx_audit_archive_entity");

        // ============================================================
        // Index 6: Filter by action
        // ============================================================
        builder.HasIndex(e => e.Action)
            .HasDatabaseName("idx_audit_archive_action");
    }
}