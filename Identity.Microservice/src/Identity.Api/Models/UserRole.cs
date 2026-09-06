using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Identity.Api.Models;

[Table("user_roles")]
public class UserRole
{
    [Key]
    [Column("id")]
    public Guid Id { get; set; }

    [Column("user_id")]
    public Guid? UserId { get; set; }

    [Column("role_id")]
    public Guid? RoleId { get; set; }

    [Column("assigned_at")]
    public DateTime? AssignedAt { get; set; } = DateTime.UtcNow;

    public User? User { get; set; }
    public Role? Role { get; set; }
}
