using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Identity.Api.Models;

/// <summary>
/// Represents the user entity in the database.
/// </summary>
[Table("users")]
public class User
{
    /// <summary>
    /// Gets or sets the unique identifier for the user.
    /// </summary>
    [Key]
    [Column("id")]
    public Guid Id { get; set; }

    /// <summary>
    /// Gets or sets the username.
    /// </summary>
    [Column("username")]
    [Required]
    [MaxLength(100)]
    public string Username { get; set; } = null!;

    /// <summary>
    /// Gets or sets the email address.
    /// </summary>
    [Column("email")]
    [Required]
    [MaxLength(255)]
    public string Email { get; set; } = null!;

    /// <summary>
    /// Gets or sets the phone number.
    /// </summary>
    [Column("phone_number")]
    [MaxLength(20)]
    public string? PhoneNumber { get; set; }

    /// <summary>
    /// Gets or sets the hashed password.
    /// </summary>
    [Column("password_hash")]
    [Required]
    public string PasswordHash { get; set; } = null!;

    /// <summary>
    /// Gets or sets a value indicating whether the user account is active.
    /// </summary>
    [Column("is_active")]
    public bool IsActive { get; set; } = true;

    /// <summary>
    /// Gets or sets a value indicating whether the user must change their password on next login.
    /// </summary>
    [Column("must_change_password")]
    public bool MustChangePassword { get; set; } = false;

    /// <summary>
    /// Gets or sets the number of consecutive failed login attempts.
    /// </summary>
    [Column("failed_login_attempts")]
    public int FailedLoginAttempts { get; set; } = 0;

    /// <summary>
    /// Gets or sets the date and time when the account lockout ends.
    /// </summary>
    [Column("lockout_end")]
    public DateTime? LockoutEnd { get; set; }

    /// <summary>
    /// Gets or sets the first name.
    /// </summary>
    [Column("first_name")]
    [MaxLength(100)]
    public string? FirstName { get; set; }

    /// <summary>
    /// Gets or sets the last name.
    /// </summary>
    [Column("last_name")]
    [MaxLength(100)]
    public string? LastName { get; set; }

    /// <summary>
    /// Gets or sets the date and time when the user was created.
    /// </summary>
    [Column("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Gets or sets the date and time when the user was last updated.
    /// </summary>
    [Column("updated_at")]
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Gets or sets the collection of user roles.
    /// </summary>
    public ICollection<UserRole> UserRoles { get; set; } = new List<UserRole>();

    /// <summary>
    /// Gets or sets the collection of user groups.
    /// </summary>
    public ICollection<UserGroup> UserGroups { get; set; } = new List<UserGroup>();

    /// <summary>
    /// Gets or sets the collection of refresh tokens.
    /// </summary>
    public ICollection<RefreshToken> RefreshTokens { get; set; } = new List<RefreshToken>();
}