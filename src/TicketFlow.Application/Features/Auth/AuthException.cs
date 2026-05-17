namespace TicketFlow.Application.Features.Auth;

public sealed class AuthException : Exception
{
    private AuthException(string code, string message)
        : base(message)
    {
        Code = code;
    }

    public string Code { get; }

    public static AuthException EmailAlreadyRegistered() =>
        new("email_already_registered", "Email is already registered.");

    public static AuthException InvalidCredentials() =>
        new("invalid_credentials", "Email or password is invalid.");

    public static AuthException InvalidRefreshToken() =>
        new("invalid_refresh_token", "Refresh token is invalid.");

    public static AuthException RefreshTokenExpired() =>
        new("refresh_token_expired", "Refresh token is expired.");

    public static AuthException UserBlocked() =>
        new("user_blocked", "User is blocked.");

    public static AuthException RoleNotAllowed() =>
        new("role_not_allowed", "This role cannot be used during registration.");
}
