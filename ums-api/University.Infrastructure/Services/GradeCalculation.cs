using University.Shared.Enums;

namespace University.Infrastructure.Services;

public static class GradeCalculation
{
    public static decimal? ComputeTotal(decimal? midterm, decimal? final) =>
        (midterm.HasValue || final.HasValue)
            ? decimal.Round(((midterm ?? 0) + (final ?? 0)) / 2m, 2)
            : null;

    public static GradeLetter? DetermineLetter(decimal? total)
    {
        if (!total.HasValue)
        {
            return null;
        }

        var t = total.Value;
        if (t >= 90) return GradeLetter.A;
        if (t >= 80) return GradeLetter.B;
        if (t >= 70) return GradeLetter.C;
        if (t >= 60) return GradeLetter.D;
        return GradeLetter.F;
    }

    public static decimal? DeterminePoints(decimal? total)
    {
        var letter = DetermineLetter(total);
        if (letter == null)
        {
            return null;
        }

        return letter switch
        {
            GradeLetter.A => 4.0m,
            GradeLetter.B => 3.0m,
            GradeLetter.C => 2.0m,
            GradeLetter.D => 1.0m,
            _ => 0.0m
        };
    }
}
