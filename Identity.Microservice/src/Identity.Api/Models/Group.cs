using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace Identity.Api.Models;

[Table("groups")]
public class Group
{
    [Key]
    [Column("id")]
    public Guid Id { get; set; }

    [Column("name")]
    [Required, MaxLength(255)]
    public string Name { get; set; } = null!; // العمود الفعلي في قاعدة البيانات

    // خاصية مساعدة باسم GroupName لتشير إلى Name دون تكرار الاسم أو التأثير على قاعدة البيانات
    [NotMapped]
    [JsonIgnore]
    public string GroupName
    {
        get => Name;
        set => Name = value;
    }

    [Column("display_name")]
    [MaxLength(255)]
    public string? DisplayName { get; set; }

    [Column("description")]
    public string? Description { get; set; }

    [Column("branch_code")]
    public string? BranchCode { get; set; }

    [Column("is_active")]
    public bool IsActive { get; set; } = true;

    [Column("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [Column("updated_at")]
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    public ICollection<UserGroup> UserGroups { get; set; } = new List<UserGroup>();
}