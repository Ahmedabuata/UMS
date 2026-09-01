using University.Shared.Common;

namespace University.Core.Entities;

public class TuitionFee : BaseEntity
{
    public Guid MajorId { get; set; }
    public string AcademicYear { get; set; } = string.Empty;
    public decimal CreditHourPrice { get; set; }

    public Major? Major { get; set; }
}
