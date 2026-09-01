namespace University.Core.Rules;

public class CourseDeletionRule : IBusinessRule
{
    private readonly bool _hasActiveEnrollments;

    public CourseDeletionRule(bool hasActiveEnrollments)
    {
        _hasActiveEnrollments = hasActiveEnrollments;
    }

    public string Error => "Cannot delete course with active enrollments.";

    public Task<bool> IsSatisfiedAsync() =>
        Task.FromResult(!_hasActiveEnrollments);
}
