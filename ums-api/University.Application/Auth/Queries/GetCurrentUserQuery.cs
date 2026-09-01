using University.Application.Abstractions;
using University.Core.Interfaces.Services;
using University.Shared.Common;
using University.Shared.DTOs.Auth;

namespace University.Application.Auth.Queries;

public class GetCurrentUserQuery : IQuery<AuthResponseDto>
{
    public Guid UserId { get; set; }
    public string Token { get; set; } = string.Empty;
}

public class GetCurrentUserQueryHandler : IQueryHandler<GetCurrentUserQuery, AuthResponseDto>
{
    private readonly IAuthService _authService;

    public GetCurrentUserQueryHandler(IAuthService authService)
    {
        _authService = authService;
    }

    public Task<Result<AuthResponseDto>> HandleAsync(
        GetCurrentUserQuery query, CancellationToken cancellationToken = default)
        => _authService.ValidateTokenAsync(query.Token)
            .ContinueWith(t => Result<AuthResponseDto>.Success(new AuthResponseDto
            {
                Token = query.Token,
                ExpiresAt = DateTime.UtcNow.AddHours(1)
            }), cancellationToken);
}
