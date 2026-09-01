namespace University.Core.Rules;

public class StudentCannotExceedCreditHoursRule : IBusinessRule
{
    private readonly int _currentCreditHours;
    private readonly int _additionalCreditHours;
    private readonly int _maxCreditHours;

    public StudentCannotExceedCreditHoursRule(int currentCreditHours, int additionalCreditHours, int maxCreditHours)
    {
        _currentCreditHours = currentCreditHours;
        _additionalCreditHours = additionalCreditHours;
        _maxCreditHours = maxCreditHours;
    }

    public string Error => $"Student would exceed maximum credit hours ({_maxCreditHours}). " +
                           $"Current: {_currentCreditHours}, Adding: {_additionalCreditHours}.";

    public Task<bool> IsSatisfiedAsync() =>
        Task.FromResult(_currentCreditHours + _additionalCreditHours <= _maxCreditHours);
}
