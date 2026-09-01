using University.Core.Entities;
using University.Shared.Enums;

namespace University.Core.Rules;

public static class GpaCalculationRule
{
    public static AcademicStanding DetermineStanding(decimal cumulativeGpa)
    {
        if (cumulativeGpa >= 3.5m)
        {
            return AcademicStanding.HONORS;
        }

        if (cumulativeGpa >= 3.0m)
        {
            return AcademicStanding.DEANS_LIST;
        }

        if (cumulativeGpa >= 2.0m)
        {
            return AcademicStanding.GOOD_STANDING;
        }

        if (cumulativeGpa >= 1.0m)
        {
            return AcademicStanding.PROBATION;
        }

        return AcademicStanding.SUSPENDED;
    }

    // Weighted GPA = sum(grade_point * credit_hours) / sum(credit_hours) for COMPLETED only
    public static decimal CalculateWeightedGpa(IReadOnlyCollection<(decimal GradePoint, int CreditHours, EnrollmentStatus Status)> entries)
    {
        var completed = entries.Where(e => e.Status == EnrollmentStatus.COMPLETED).ToList();
        if (completed.Count == 0)
        {
            return 0m;
        }

        var totalPoints = completed.Sum(e => e.GradePoint * e.CreditHours);
        var totalCredits = completed.Sum(e => e.CreditHours);

        if (totalCredits == 0)
        {
            return 0m;
        }

        return decimal.Round(totalPoints / totalCredits, 2);
    }
}
