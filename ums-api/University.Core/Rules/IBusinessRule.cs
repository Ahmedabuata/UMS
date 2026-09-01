namespace University.Core.Rules;

public interface IBusinessRule
{
    Task<bool> IsSatisfiedAsync();
    string Error { get; }
}
