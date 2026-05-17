namespace TicketFlow.Application.Features.Auth;

public interface IAuthService
{
    Task<AuthResult> RegisterAsync(RegisterCommand command, CancellationToken cancellationToken = default);

    Task<AuthResult> LoginAsync(LoginCommand command, CancellationToken cancellationToken = default);

    Task<AuthResult> RefreshAsync(RefreshTokenCommand command, CancellationToken cancellationToken = default);

    Task LogoutAsync(LogoutCommand command, CancellationToken cancellationToken = default);
}
