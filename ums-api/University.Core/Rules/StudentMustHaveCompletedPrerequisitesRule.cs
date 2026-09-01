using University.Core.Entities;

namespace University.Core.Rules;

public class StudentMustHaveCompletedPrerequisitesRule : IBusinessRule
{
    private readonly IReadOnlyCollection<CoursePrerequisite> _prerequisites;
    private readonly IReadOnlyCollection<Grade> _completedGrades;

    public StudentMustHaveCompletedPrerequisitesRule(
        IReadOnlyCollection<CoursePrerequisite> prerequisites,
        IReadOnlyCollection<Grade> completedGrades)
    {
        _prerequisites = prerequisites;
        _completedGrades = completedGrades;
    }

    public string Error => "Student has not completed all mandatory prerequisites.";

    public Task<bool> IsSatisfiedAsync()
    {
        foreach (var prereq in _prerequisites)
        {
            if (!prereq.IsMandatory)
            {
                continue;
            }

            var passed = _completedGrades.Any(g =>
                !g.IsLocked == false &&
                g.EnrollmentId != Guid.Empty &&
                g.LetterGrade.HasValue &&
                g.LetterGrade.Value != University.Shared.Enums.GradeLetter.F &&
                g.LetterGrade.Value != University.Shared.Enums.GradeLetter.I &&
                g.LetterGrade.Value != University.Shared.Enums.GradeLetter.W);

            if (!passed)
            {
                return Task.FromResult(false);
            }
        }

        return Task.FromResult(true);
    }
}
