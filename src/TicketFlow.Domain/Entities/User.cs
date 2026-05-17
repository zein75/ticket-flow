using TicketFlow.Domain.Abstractions;
using TicketFlow.Domain.Enums;

namespace TicketFlow.Domain.Entities;

public sealed class User : Entity
{
    private readonly List<RefreshToken> _refreshTokens = [];

    private User()
    {
        Email = string.Empty;
        PasswordHash = string.Empty;
    }

    public User(Guid id, string email, string passwordHash, UserRole role)
        : base(id)
    {
        Email = string.IsNullOrWhiteSpace(email) ? throw new ArgumentException("Email is required.", nameof(email)) : email;
        PasswordHash = string.IsNullOrWhiteSpace(passwordHash)
            ? throw new ArgumentException("Password hash is required.", nameof(passwordHash))
            : passwordHash;
        Role = role;
    }

    public string Email { get; private set; }

    public string PasswordHash { get; private set; }

    public UserRole Role { get; private set; }

    public bool IsBlocked { get; private set; }

    public IReadOnlyCollection<RefreshToken> RefreshTokens => _refreshTokens.AsReadOnly();

    public void Block() => IsBlocked = true;

    public void Unblock() => IsBlocked = false;

    public void AddRefreshToken(RefreshToken refreshToken)
    {
        ArgumentNullException.ThrowIfNull(refreshToken);

        if (refreshToken.UserId != Id)
        {
            throw new ArgumentException("Refresh token belongs to another user.", nameof(refreshToken));
        }

        _refreshTokens.Add(refreshToken);
    }
}
