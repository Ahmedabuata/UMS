using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Identity.Api.Models;

[Table("permissions")]
public class Permission
{
    [Key]
    [Column("id")]
    public Guid Id { get; set; }

    [Column("permission_name")]
    [Required, MaxLength(255)]
    public string PermissionName { get; set; } = null!;

    [Column("description")]
    public string? Description { get; set; }

    [Column("module")]
    public string? Module { get; set; }

    [Column("module_code")]
    public string? ModuleCode { get; set; }

    [Column("branch_code")]
    public string? BranchCode { get; set; }

    [Column("is_sensitive")]
    public bool IsSensitive { get; set; } = false;

    [Column("is_active")]
    public bool IsActive { get; set; } = true;

    [Column("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [Column("updated_at")]
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    public ICollection<RolePermission> RolePermissions { get; set; } = new List<RolePermission>();
}
