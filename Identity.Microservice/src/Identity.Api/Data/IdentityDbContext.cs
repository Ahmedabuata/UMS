using Identity.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace Identity.Api.Data;

/// <summary>
/// Database context for the Identity microservice.
/// Manages all entities including Users, Roles, Permissions,
/// AuditLogs (Hot), and Audit Archives (Cold).
/// </summary>
public class IdentityDbContext : DbContext
{
    public IdentityDbContext(DbContextOptions<IdentityDbContext> options) : base(options) { }

    // ============================================================
    // Core Entities
    // ============================================================
    public DbSet<User> Users => Set<User>();
    public DbSet<Role> Roles => Set<Role>();
    public DbSet<Permission> Permissions => Set<Permission>();
    public DbSet<RolePermission> RolePermissions => Set<RolePermission>();
    public DbSet<UserRole> UserRoles => Set<UserRole>();
    public DbSet<Group> Groups => Set<Group>();
    public DbSet<UserGroup> UserGroups => Set<UserGroup>();
    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();
    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();
    public DbSet<UserProfile> UserProfiles => Set<UserProfile>();

    // ============================================================
    // Audit Archive Entities (Cold Storage)
    // ============================================================
    /// <summary>
    /// Archived audit logs (read-only cold storage).
    /// Contains records older than the retention threshold (e.g., 3 months).
    /// </summary>
    public DbSet<AuditLogArchive> AuditLogsArchive => Set<AuditLogArchive>();

    /// <summary>
    /// Tracks all archive job executions.
    /// Used for monitoring, idempotency, and admin dashboard.
    /// </summary>
    public DbSet<AuditArchiveJob> AuditArchiveJobs => Set<AuditArchiveJob>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // ===================================================================
        // Section 1: Unique Constraints (Prevent duplicates in join tables)
        // ===================================================================

        // Prevent assigning the same role to the same user twice.
        modelBuilder.Entity<UserRole>()
            .HasIndex(ur => new { ur.UserId, ur.RoleId })
            .IsUnique();

        // Prevent granting the same permission to the same role twice.
        modelBuilder.Entity<RolePermission>()
            .HasIndex(rp => new { rp.RoleId, rp.PermissionId })
            .IsUnique();

        // Prevent adding the same user to the same group twice.
        modelBuilder.Entity<UserGroup>()
            .HasIndex(ug => new { ug.UserId, ug.GroupId })
            .IsUnique();

        // Ensure only one profile per user (1:1 relationship).
        modelBuilder.Entity<UserProfile>()
            .HasIndex(up => up.UserId)
            .IsUnique();

        // ===================================================================
        // Section 2: Relationships (Foreign Keys)
        // ===================================================================

        modelBuilder.Entity<UserRole>()
            .HasOne(ur => ur.User)
            .WithMany(u => u.UserRoles)
            .HasForeignKey(ur => ur.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<UserRole>()
            .HasOne(ur => ur.Role)
            .WithMany(r => r.UserRoles)
            .HasForeignKey(ur => ur.RoleId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<RolePermission>()
            .HasOne(rp => rp.Role)
            .WithMany(r => r.RolePermissions)
            .HasForeignKey(rp => rp.RoleId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<RolePermission>()
            .HasOne(rp => rp.Permission)
            .WithMany(p => p.RolePermissions)
            .HasForeignKey(rp => rp.PermissionId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<UserGroup>()
            .HasOne(ug => ug.User)
            .WithMany(u => u.UserGroups)
            .HasForeignKey(ug => ug.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<UserGroup>()
            .HasOne(ug => ug.Group)
            .WithMany(g => g.UserGroups)
            .HasForeignKey(ug => ug.GroupId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<RefreshToken>()
            .HasOne(rt => rt.User)
            .WithMany(u => u.RefreshTokens)
            .HasForeignKey(rt => rt.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<UserProfile>()
            .HasOne(up => up.User)
            .WithMany()
            .HasForeignKey(up => up.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        // ===================================================================
        // Section 3: Global Query Filters (Auto-exclude inactive records)
        // ===================================================================
        // NOTE: These filters are NOT applied to archive tables (by design).
        modelBuilder.Entity<Group>().HasQueryFilter(g => g.IsActive);
        modelBuilder.Entity<Role>().HasQueryFilter(r => r.IsActive);
        modelBuilder.Entity<Permission>().HasQueryFilter(p => p.IsActive);

        // ===================================================================
        // Section 4: Core Table Indexes (Unique indexes for performance)
        // ===================================================================
        modelBuilder.Entity<User>().HasIndex(u => u.Email).IsUnique();
        modelBuilder.Entity<User>().HasIndex(u => u.Username).IsUnique();
        modelBuilder.Entity<Role>().HasIndex(r => r.RoleName).IsUnique();
        modelBuilder.Entity<Permission>().HasIndex(p => p.PermissionName).IsUnique();
        modelBuilder.Entity<Group>().HasIndex(g => g.Name).IsUnique();

        // ===================================================================
        // Section 5: AuditLog (HOT) Configuration
        // ===================================================================
        // IMPORTANT: These indexes MUST match the existing database schema 
        // (7 indexes) to avoid creating duplicates or missing optimizations.
        modelBuilder.Entity<AuditLog>(entity =>
        {
            entity.ToTable("audit_logs");
            entity.HasKey(e => e.Id);

            // Index 1: Filter by action
            entity.HasIndex(e => e.Action)
                .HasDatabaseName("idx_audit_logs_action");

            // Index 2: Latest records first
            entity.HasIndex(e => e.CreatedAt)
                .IsDescending()
                .HasDatabaseName("idx_audit_logs_created_at_desc");

            // Index 3: Lookup by entity
            entity.HasIndex(e => new { e.Entity, e.EntityId })
                .HasDatabaseName("idx_audit_logs_entity");

            // Index 4: Filter by timestamp
            entity.HasIndex(e => e.Timestamp)
                .HasDatabaseName("idx_audit_logs_timestamp");

            // Index 5: Composite index for user activity analysis
            entity.HasIndex(e => new { e.UserId, e.Action, e.CreatedAt })
                .IsDescending(false, false, true)
                .HasDatabaseName("idx_audit_logs_user_action_date");

            // Index 6: Filter by user
            entity.HasIndex(e => e.UserId)
                .HasDatabaseName("idx_audit_logs_user_id");
        });

        // ===================================================================
        // Section 6: Apply External Configurations (NEW)
        // ===================================================================
        // Automatically applies all classes implementing 
        // IEntityTypeConfiguration<T> in the current assembly.
        // 
        // This replaces Sections 6 & 7 from the previous version.
        // 
        // Applied configurations (from Configurations/ folder):
        // - AuditLogArchiveConfiguration  → table: audit_logs_archive
        // - AuditArchiveJobConfiguration  → table: audit_archive_jobs
        // 
        // BENEFITS:
        // - Separation of concerns (each entity has its own config file)
        // - Easier maintenance and testing
        // - No conflict with existing entities
        // ===================================================================
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(IdentityDbContext).Assembly);
    }
}