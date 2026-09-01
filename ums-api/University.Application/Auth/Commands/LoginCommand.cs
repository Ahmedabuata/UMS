using University.Application.Abstractions;
using University.Core.Interfaces.Services;
using University.Shared.Common;
using University.Shared.DTOs.Auth;

namespace University.Application.Auth.Commands;

public class LoginCommand : ICommand<AuthResponseDto>
{
    public string Email { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
}

public class LoginCommandHandler : ICommandHandler<LoginCommand, AuthResponseDto>
{
    private readonly IAuthService _authService;

    public LoginCommandHandler(IAuthService authService)
    {
        _authService = authService;
    }

    public Task<Result<AuthResponseDto>> HandleAsync(
        LoginCommand command, CancellationToken cancellationToken = default)
        => _authService.LoginAsync(command.Email, command.Password);
}
