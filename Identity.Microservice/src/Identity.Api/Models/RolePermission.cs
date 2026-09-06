using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Identity.Api.Models;

[Table("role_permissions")]
public class RolePermission
{
    [Key]
    [Column("id")]
    public Guid Id { get; set; }

    [Column("role_id")]
    public Guid? RoleId { get; set; }

    [Column("permission_id")]
    public Guid? PermissionId { get; set; }

    [Column("branch_code")]
    public string? BranchCode { get; set; }

    [Column("granted_by")]
    public Guid? GrantedBy { get; set; }

    [Column("granted_at")]
    public DateTime? GrantedAt { get; set; } = DateTime.UtcNow;

    [Column("is_active")]
    public bool IsActive { get; set; } = true;

    [Column("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [Column("updated_at")]
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    public Role? Role { get; set; }
    public Permission? Permission { get; set; }
}
