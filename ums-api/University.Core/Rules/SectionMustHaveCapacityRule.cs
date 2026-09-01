using University.Core.Entities;

namespace University.Core.Rules;

public class SectionMustHaveCapacityRule : IBusinessRule
{
    private readonly CourseSection _section;

    public SectionMustHaveCapacityRule(CourseSection section)
    {
        _section = section;
    }

    public string Error => "The section has reached its maximum capacity.";

    public Task<bool> IsSatisfiedAsync() =>
        Task.FromResult(_section.IsActive && _section.CurrentEnrollment < _section.MaxCapacity);
}
