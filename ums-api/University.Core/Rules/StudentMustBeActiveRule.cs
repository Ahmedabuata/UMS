using University.Core.Entities;
using University.Shared.Enums;

namespace University.Core.Rules;

public class StudentMustBeActiveRule : IBusinessRule
{
    private readonly Student _student;

    public StudentMustBeActiveRule(Student student)
    {
        _student = student;
    }

    public string Error => "Student is not active and cannot enroll.";

    public Task<bool> IsSatisfiedAsync() =>
        Task.FromResult(_student.IsActive && _student.Status == StudentStatus.ST_ACTIVE);
}
