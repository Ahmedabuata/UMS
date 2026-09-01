using University.Application.Abstractions;
using University.Core.Interfaces.Services;
using University.Shared.Common;
using University.Shared.DTOs.Auth;

namespace University.Application.Auth.Commands;

public class RegisterCommand : ICommand<AuthResponseDto>
{
    public RegisterRequestDto Dto { get; set; } = new();
}

public class RegisterCommandHandler : ICommandHandler<RegisterCommand, AuthResponseDto>
{
    private readonly IAuthService _authService;

    public RegisterCommandHandler(IAuthService authService)
    {
        _authService = authService;
    }

    public Task<Result<AuthResponseDto>> HandleAsync(
        RegisterCommand command, CancellationToken cancellationToken = default)
        => _authService.RegisterAsync(command.Dto);
}
