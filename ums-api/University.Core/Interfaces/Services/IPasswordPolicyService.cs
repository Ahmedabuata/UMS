namespace University.Core.Interfaces.Services;

public interface IPasswordPolicyService
{
    string GenerateTempPassword();
    (bool Valid, string Reason) ValidatePassword(string password);
}