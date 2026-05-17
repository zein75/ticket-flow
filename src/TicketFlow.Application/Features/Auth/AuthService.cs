using Microsoft.Extensions.Options;
using TicketFlow.Application.Abstractions.Auth;
using TicketFlow.Application.Abstractions.Common;
using TicketFlow.Application.Abstractions.Persistence;
using TicketFlow.Domain.Entities;
using TicketFlow.Domain.Enums;

namespace TicketFlow.Application.Features.Auth;

public sealed class AuthService : IAuthService
{
    private readonly IUserRepository _users;
    private readonly IRefreshTokenRepository _refreshTokens;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IPasswordHasher _passwordHasher;
    private readonly ITokenService _tokenService;
    private readonly IRefreshTokenGenerator _refreshTokenGenerator;
    private readonly IClock _clock;
    private readonly AuthOptions _options;

    public AuthService(
        IUserRepository users,
        IRefreshTokenRepository refreshTokens,
        IUnitOfWork unitOfWork,
        IPasswordHasher passwordHasher,
        ITokenService tokenService,
        IRefreshTokenGenerator refreshTokenGenerator,
        IClock clock,
        IOptions<AuthOptions> options)
    {
        _users = users;
        _refreshTokens = refreshTokens;
        _unitOfWork = unitOfWork;
        _passwordHasher = passwordHasher;
        _tokenService = tokenService;
        _refreshTokenGenerator = refreshTokenGenerator;
        _clock = clock;
        _options = options.Value;
    }

    public async Task<AuthResult> RegisterAsync(RegisterCommand command, CancellationToken cancellationToken = default)
    {
        var email = NormalizeEmail(command.Email);
        var existingUser = await _users.GetByEmailAsync(email, cancellationToken);
        if (existingUser is not null)
        {
            throw AuthException.EmailAlreadyRegistered();
        }

        var user = new User(Guid.NewGuid(), email, _passwordHasher.Hash(command.Password), UserRole.User);
        _users.Add(user);

        var result = IssueTokens(user);
        user.AddRefreshToken(new RefreshToken(
            Guid.NewGuid(),
            user.Id,
            _refreshTokenGenerator.Hash(result.RefreshToken),
            _clock.UtcNow,
            result.RefreshTokenExpiresAtUtc));

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return result;
    }

    public async Task<AuthResult> LoginAsync(LoginCommand command, CancellationToken cancellationToken = default)
    {
        var email = NormalizeEmail(command.Email);
        var user = await _users.GetByEmailAsync(email, cancellationToken);
        if (user is null || !_passwordHasher.Verify(command.Password, user.PasswordHash))
        {
            throw AuthException.InvalidCredentials();
        }

        if (user.IsBlocked)
        {
            throw AuthException.UserBlocked();
        }

        var result = IssueTokens(user);
        user.AddRefreshToken(new RefreshToken(
            Guid.NewGuid(),
            user.Id,
            _refreshTokenGenerator.Hash(result.RefreshToken),
            _clock.UtcNow,
            result.RefreshTokenExpiresAtUtc));

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return result;
    }

    public async Task<AuthResult> RefreshAsync(RefreshTokenCommand command, CancellationToken cancellationToken = default)
    {
        var tokenHash = _refreshTokenGenerator.Hash(command.RefreshToken);
        var refreshToken = await _refreshTokens.GetByTokenHashAsync(tokenHash, cancellationToken)
            ?? throw AuthException.InvalidRefreshToken();

        if (!refreshToken.IsActive(_clock.UtcNow))
        {
            throw AuthException.RefreshTokenExpired();
        }

        var user = await _users.GetByIdAsync(refreshToken.UserId, cancellationToken)
            ?? throw AuthException.InvalidRefreshToken();

        if (user.IsBlocked)
        {
            throw AuthException.UserBlocked();
        }

        var result = IssueTokens(user);
        refreshToken.Revoke(_clock.UtcNow, _refreshTokenGenerator.Hash(result.RefreshToken));
        user.AddRefreshToken(new RefreshToken(
            Guid.NewGuid(),
            user.Id,
            _refreshTokenGenerator.Hash(result.RefreshToken),
            _clock.UtcNow,
            result.RefreshTokenExpiresAtUtc));

        try
        {
            await _unitOfWork.SaveChangesAsync(cancellationToken);
        }
        catch (PersistenceConcurrencyException)
        {
            throw AuthException.InvalidRefreshToken();
        }

        return result;
    }

    public async Task LogoutAsync(LogoutCommand command, CancellationToken cancellationToken = default)
    {
        var tokenHash = _refreshTokenGenerator.Hash(command.RefreshToken);
        var refreshToken = await _refreshTokens.GetByTokenHashAsync(tokenHash, cancellationToken);
        if (refreshToken is null)
        {
            return;
        }

        refreshToken.Revoke(_clock.UtcNow);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }

    private AuthResult IssueTokens(User user)
    {
        var accessTokenExpiresAtUtc = _clock.UtcNow.AddMinutes(_options.AccessTokenMinutes);
        var refreshTokenExpiresAtUtc = _clock.UtcNow.AddDays(_options.RefreshTokenDays);

        return new AuthResult(
            user.Id,
            user.Email,
            user.Role,
            _tokenService.CreateAccessToken(user),
            _refreshTokenGenerator.Generate(),
            accessTokenExpiresAtUtc,
            refreshTokenExpiresAtUtc);
    }

    private static string NormalizeEmail(string email)
    {
        if (string.IsNullOrWhiteSpace(email))
        {
            throw AuthException.InvalidCredentials();
        }

        return email.Trim().ToLowerInvariant();
    }
}
