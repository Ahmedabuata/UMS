namespace Identity.Contracts.Auth;

public record LoginRequest(string Username, string Password);
public record RefreshRequest(string RefreshToken);
public record ChangePasswordRequest(string OldPassword, string NewPassword);
public record ForceChangePasswordRequest(string NewPassword);
