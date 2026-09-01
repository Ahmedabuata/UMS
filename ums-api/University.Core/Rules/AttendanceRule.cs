using University.Shared.Constants;

namespace University.Core.Rules;

public class AttendanceRule
{
    public enum AttendanceWarningType
    {
        NONE = 0,
        WARNING_1 = 1,
        WARNING_2 = 2,
        DISMISSED = 3
    }

    // DN>25% dismissal warnings at 10% / 15%
    public static (AttendanceWarningType Warning, string Message) EvaluateAbsence(
        int totalSessions, int absentSessions)
    {
        if (totalSessions <= 0)
        {
            return (AttendanceWarningType.NONE, string.Empty);
        }

        var absencePercent = (decimal)absentSessions / totalSessions * 100m;

        if (absencePercent > GradeConstants.DismissalAbsencePercent)
        {
            return (AttendanceWarningType.DISMISSED,
                $"Student dismissed for exceeding {GradeConstants.DismissalAbsencePercent}% absence.");
        }

        if (absencePercent >= GradeConstants.AbsenceWarningThresholdPercent2)
        {
            return (AttendanceWarningType.WARNING_2,
                $"Warning: absence is {absencePercent:0.#}%, approaching dismissal threshold.");
        }

        if (absencePercent >= GradeConstants.AbsenceWarningThresholdPercent1)
        {
            return (AttendanceWarningType.WARNING_1,
                $"First warning: absence is {absencePercent:0.#}%.");
        }

        return (AttendanceWarningType.NONE, string.Empty);
    }
}
