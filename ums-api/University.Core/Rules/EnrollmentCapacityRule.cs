using University.Core.Entities;

namespace University.Core.Rules;

public class EnrollmentCapacityRule : IBusinessRule
{
    private readonly CourseSection _section;

    public EnrollmentCapacityRule(CourseSection section)
    {
        _section = section;
    }

    public string Error => "Section is at full capacity.";

    public Task<bool> IsSatisfiedAsync() =>
        // Uses FOR UPDATE in repository to lock the row for concurrency
        Task.FromResult(_section.CurrentEnrollment < _section.MaxCapacity);
}
