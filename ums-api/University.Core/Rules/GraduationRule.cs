using University.Shared.Constants;

namespace University.Core.Rules;

public class GraduationRule
{
    // Credits + CGPA >= 2.0 + clearance
    public static (bool IsEligible, string Message) Evaluate(
        int requiredCredits, int completedCredits,
        decimal cumulativeGpa, bool financialClearance, bool academicClearance)
    {
        if (completedCredits < requiredCredits)
        {
            return (false, $"Insufficient credits: {completedCredits}/{requiredCredits}.");
        }

        if (cumulativeGpa < StudentConstants.MinCumulativeGpaForGraduation)
        {
            return (false, $"CGPA {cumulativeGpa} is below the minimum {StudentConstants.MinCumulativeGpaForGraduation}.");
        }

        if (!financialClearance)
        {
            return (false, "Financial clearance is not complete.");
        }

        if (!academicClearance)
        {
            return (false, "Academic clearance is not complete.");
        }

        return (true, "Eligible for graduation.");
    }
}
