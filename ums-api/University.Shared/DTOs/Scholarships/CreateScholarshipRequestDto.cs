namespace University.Shared.DTOs.Scholarships;

public class CreateScholarshipRequestDto
{
    public string ScholarshipName { get; set; } = string.Empty;
    public string ScholarshipCode { get; set; } = string.Empty;
    public decimal DiscountPercentage { get; set; }
    public decimal? MaxAmount { get; set; }
}
