using University.Shared.Common;

namespace University.Core.Entities;

public class Scholarship : BaseEntity
{
    public string ScholarshipName { get; set; } = string.Empty;
    public string ScholarshipCode { get; set; } = string.Empty;
    public decimal DiscountPercentage { get; set; }
    public decimal? MaxAmount { get; set; }
}
