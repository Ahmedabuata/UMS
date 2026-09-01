using System.Text;
using University.Shared.Constants;

namespace University.Core.Rules;

public class CreditHoursRule
{
    public enum LoadWarningType
    {
        NONE = 0,
        WARN_1 = 1,
        WARN_2 = 2,
        UNDERLOAD = 3
    }

    // ST-ACTIVE: 12-18 credits (21 graduate); WARN-1 at 14; WARN-2 at 12 underload
    public static (bool IsValid, LoadWarningType Warning, string Message) Validate(
        int totalCreditHours, bool isGraduate)
    {
        if (totalCreditHours < StudentConstants.UnderLoadMinCredits)
        {
            return (false, LoadWarningType.WARN_2,
                $"Below minimum load. Minimum is {StudentConstants.UnderLoadMinCredits} credit hours.");
        }

        var maxAllowed = isGraduate ? StudentConstants.GraduateCreditHours : StudentConstants.MaxCreditHours;

        if (totalCreditHours > maxAllowed)
        {
            return (false, LoadWarningType.WARN_1,
                $"Exceeds maximum credit hours of {maxAllowed}.");
        }

        return (true, LoadWarningType.NONE, string.Empty);
    }

    public static bool HasScheduleConflict(
        string? existingScheduleDays, string? newScheduleDays,
        TimeOnly? existingStart, TimeOnly? existingEnd,
        TimeOnly? newStart, TimeOnly? newEnd)
    {
        if (string.IsNullOrWhiteSpace(existingScheduleDays) ||
            string.IsNullOrWhiteSpace(newScheduleDays) ||
            !existingStart.HasValue || !existingEnd.HasValue ||
            !newStart.HasValue || !newEnd.HasValue)
        {
            return false;
        }

        var existingDays = new HashSet<string>(existingScheduleDays.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries));
        var newDays = new HashSet<string>(newScheduleDays.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries));

        if (!existingDays.Overlaps(newDays))
        {
            return false;
        }

        return existingStart.Value < newEnd.Value && newStart.Value < existingEnd.Value;
    }
}
