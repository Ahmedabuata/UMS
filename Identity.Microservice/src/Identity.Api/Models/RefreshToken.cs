using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Identity.Api.Models;

[Table("refresh_tokens")]
public class RefreshToken
{
    [Key]
    [Column("id")]
    public Guid Id { get; set; }

    [Column("user_id")]
    public Guid UserId { get; set; }

    [Column("token_hash")]
    [Required]
    public string TokenHash { get; set; } = null!;

    [Column("ip_address")]
    public string? IpAddress { get; set; }

    [Column("user_agent")]
    public string? UserAgent { get; set; }

    [Column("expires_at")]
    public DateTime ExpiresAt { get; set; }

    [Column("revoked")]
    public bool Revoked { get; set; } = false;

    [Column("revoked_at")]
    public DateTime? RevokedAt { get; set; }

    [Column("revoked_by_ip")]
    public string? RevokedByIp { get; set; }

    [Column("replaced_by_token")]
    public string? ReplacedByToken { get; set; }

    [Column("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Foreign Key Navigation
    [ForeignKey(nameof(UserId))]
    public User? User { get; set; }

    // Calculated Helpers
    [NotMapped]
    public bool IsExpired => DateTime.UtcNow >= ExpiresAt;

    [NotMapped]
    public bool IsActive => !Revoked && !IsExpired;

    // Backward Compatibility Aliases
    [NotMapped]
    public string Token => TokenHash;

    [NotMapped]
    public string TokenFamily => Id.ToString();

    [NotMapped]
    public string? CreatedByIp => IpAddress;
}