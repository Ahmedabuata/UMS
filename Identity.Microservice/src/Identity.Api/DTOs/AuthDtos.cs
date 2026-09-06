namespace Identity.Api.DTOs;

public record RegisterRequest(string Username, string Email, string Password, string? PhoneNumber);
public record LoginRequest(string EmailOrUsername, string Password);
public record RefreshTokenRequest(string RefreshToken);
public record RevokeTokenRequest(string? RefreshToken);
public record AuthResponse(string AccessToken, string RefreshToken, DateTime AccessTokenExpiresAt, DateTime RefreshTokenExpiresAt, UserDto User);
public record MeResponse(UserDto User);
