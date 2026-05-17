using TicketFlow.Domain.Abstractions;
using TicketFlow.Domain.Enums;

namespace TicketFlow.Domain.Entities;

public sealed class User : Entity
{
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

    public void Block() => IsBlocked = true;

    public void Unblock() => IsBlocked = false;
}
