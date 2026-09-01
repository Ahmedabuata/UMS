using University.Core.Entities;

namespace University.Core.Rules;

public class SemesterMustBeValidRule : IBusinessRule
{
    private readonly Semester _semester;
    private readonly DateOnly _today;

    public SemesterMustBeValidRule(Semester semester, DateOnly? today = null)
    {
        _semester = semester;
        _today = today ?? DateOnly.FromDateTime(DateTime.UtcNow);
    }

    public string Error => "Semester is not valid for enrollment.";

    public Task<bool> IsSatisfiedAsync() =>
        Task.FromResult(_semester.IsActive && (_semester.IsCurrent ||
            (_today >= _semester.StartDate && _today <= _semester.EndDate)));
}
