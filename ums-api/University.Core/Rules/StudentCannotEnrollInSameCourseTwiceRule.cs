using University.Core.Entities;

namespace University.Core.Rules;

public class StudentCannotEnrollInSameCourseTwiceRule : IBusinessRule
{
    private readonly CourseEnrollment _existing;

    public StudentCannotEnrollInSameCourseTwiceRule(CourseEnrollment? existing)
    {
        _existing = existing ?? new CourseEnrollment();
    }

    public string Error => "Student is already enrolled in this course section for this semester.";

    public Task<bool> IsSatisfiedAsync()
    {
        // Satisfied when no duplicate enrollment exists
        return Task.FromResult(_existing.Id == Guid.Empty);
    }
}
