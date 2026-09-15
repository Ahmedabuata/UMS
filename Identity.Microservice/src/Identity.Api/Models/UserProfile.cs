using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Identity.Api.Models;

/// <summary>
/// Represents the user profile entity in the database.
/// </summary>
[Table("user_profiles")]
public class UserProfile
{
    /// <summary>
    /// Gets or sets the unique identifier for the user profile.
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
    /// Gets or sets the street address.
    /// </summary>
    [Column("address")]
    [MaxLength(255)]
    public string? Address { get; set; }

    /// <summary>
    /// Gets or sets the city.
    /// </summary>
    [Column("city")]
    [MaxLength(100)]
    public string? City { get; set; }

    /// <summary>
    /// Gets or sets the postal code.
    /// </summary>
    [Column("postal_code")]
    [MaxLength(20)]
    public string? PostalCode { get; set; }

    /// <summary>
    /// Gets or sets the marital status.
    /// </summary>
    [Column("marital_status")]
    [MaxLength(50)]
    public string? MaritalStatus { get; set; }

    /// <summary>
    /// Gets or sets the date of birth.
    /// </summary>
    [Column("date_of_birth")]
    public DateTime? DateOfBirth { get; set; }

    /// <summary>
    /// Gets or sets the gender.
    /// </summary>
    [Column("gender")]
    [MaxLength(20)]
    public string? Gender { get; set; }

    /// <summary>
    /// Gets or sets the date and time when the profile was created.
    /// </summary>
    [Column("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Gets or sets the date and time when the profile was last updated.
    /// </summary>
    [Column("updated_at")]
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Gets or sets the associated user.
    /// </summary>
    [ForeignKey(nameof(UserId))]
    public User? User { get; set; }
}