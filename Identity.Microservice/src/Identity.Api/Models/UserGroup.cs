using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Identity.Api.Models;

/// <summary>
/// Represents the association between a user and a group.
/// </summary>
[Table("user_groups")]
public class UserGroup
{
    /// <summary>
    /// Gets or sets the unique identifier for the user-group association.
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
    /// Gets or sets the group identifier.
    /// </summary>
    [Column("group_id")]
    public Guid GroupId { get; set; }

    /// <summary>
    /// Gets or sets the date and time when the user joined the group.
    /// </summary>
    [Column("joined_at")]
    public DateTime JoinedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Gets or sets the date and time when the user was assigned to the group (Alias for JoinedAt).
    /// </summary>
    [NotMapped]
    public DateTime AssignedAt
    {
        get => JoinedAt;
        set => JoinedAt = value;
    }

    /// <summary>
    /// Gets or sets the associated user.
    /// </summary>
    [ForeignKey(nameof(UserId))]
    public User? User { get; set; }

    /// <summary>
    /// Gets or sets the associated group.
    /// </summary>
    [ForeignKey(nameof(GroupId))]
    public Group? Group { get; set; }
}