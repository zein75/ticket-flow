using TicketFlow.Domain.Enums;

namespace TicketFlow.Application.Features.Auth;

public sealed record AuthResult(
    Guid UserId,
    string Email,
    UserRole Role,
    string AccessToken,
    string RefreshToken,
    DateTime AccessTokenExpiresAtUtc,
    DateTime RefreshTokenExpiresAtUtc);
