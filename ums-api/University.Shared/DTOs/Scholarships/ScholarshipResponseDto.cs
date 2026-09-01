namespace University.Shared.DTOs.Scholarships;

public class ScholarshipResponseDto
{
    public Guid Id { get; set; }
    public string ScholarshipName { get; set; } = string.Empty;
    public string ScholarshipCode { get; set; } = string.Empty;
    public decimal DiscountPercentage { get; set; }
    public decimal? MaxAmount { get; set; }
    public bool IsActive { get; set; }
}
