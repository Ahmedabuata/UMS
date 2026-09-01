using University.Core.Entities;
using University.Shared.Enums;

namespace University.Core.Rules;

public class FinancialHoldRule : IBusinessRule
{
    private readonly bool _hasOutstandingBalance;

    public FinancialHoldRule(bool hasOutstandingBalance)
    {
        _hasOutstandingBalance = hasOutstandingBalance;
    }

    public string Error =>
        "ST-SUSP-FIN: Student has a financial hold. Transcript, exam, and add/drop are blocked until balance is cleared.";

    public Task<bool> IsSatisfiedAsync() =>
        Task.FromResult(!_hasOutstandingBalance);
}
