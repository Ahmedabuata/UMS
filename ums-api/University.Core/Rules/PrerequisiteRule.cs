using University.Core.Entities;
using University.Shared.Enums;

namespace University.Core.Rules;

public class PrerequisiteRule : IBusinessRule
{
    private readonly IReadOnlyCollection<CoursePrerequisite> _mandatoryPrerequisites;
    private readonly IReadOnlyCollection<GradeLetter> _passedGrades;

    public PrerequisiteRule(
        IReadOnlyCollection<CoursePrerequisite> mandatoryPrerequisites,
        IReadOnlyCollection<GradeLetter> passedGrades)
    {
        _mandatoryPrerequisites = mandatoryPrerequisites;
        _passedGrades = passedGrades;
    }

    public string Error => "Mandatory prerequisites have not been completed.";

    public Task<bool> IsSatisfiedAsync()
    {
        if (_mandatoryPrerequisites.Count == 0)
        {
            return Task.FromResult(true);
        }

        foreach (var prereq in _mandatoryPrerequisites)
        {
            var passed = _passedGrades.Contains(GradeLetter.A) ||
                         _passedGrades.Contains(GradeLetter.B) ||
                         _passedGrades.Contains(GradeLetter.C) ||
                         _passedGrades.Contains(GradeLetter.D);

            if (!passed)
            {
                // TODO: cross-reference prereq.course with the grade's enrollment course
                return Task.FromResult(false);
            }
        }

        return Task.FromResult(true);
    }
}
