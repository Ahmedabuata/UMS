using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Identity.Api.Models;

/// <summary>
/// Represents the association between a user and a role.
/// </summary>
[Table("user_roles")]
public class UserRole
{
    /// <summary>
    /// Gets or sets the unique identifier for the user-role association.
    /// </summary>
    [Key]
    [Column("id")]
    public Guid Id { get; set; } = Guid.NewGuid();

    /// <summary>
    /// Gets or sets the user identifier.
    /// </summary>
    [Column("user_id")]
    public Guid UserId { get; set; }

    /// <summary>
    /// Gets or sets the role identifier.
    /// </summary>
    [Column("role_id")]
    public Guid RoleId { get; set; }

    /// <summary>
    /// Gets or sets the date and time when the role was assigned to the user.
    /// </summary>
    [Column("assigned_at")]
    public DateTime AssignedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Gets or sets the associated user.
    /// </summary>
    [ForeignKey(nameof(UserId))]
    public User? User { get; set; }

    /// <summary>
    /// Gets or sets the associated role.
    /// </summary>
    [ForeignKey(nameof(RoleId))]
    public Role? Role { get; set; }
}