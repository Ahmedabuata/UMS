using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Identity.Api.Models;

[Table("user_groups")]
public class UserGroup
{
    [Key]
    [Column("id")]
    public Guid Id { get; set; }

    [Column("user_id")]
    public Guid? UserId { get; set; }

    [Column("group_id")]
    public Guid? GroupId { get; set; }

    [Column("joined_at")]
    public DateTime? JoinedAt { get; set; } = DateTime.UtcNow;

    public User? User { get; set; }
    public Group? Group { get; set; }
}
