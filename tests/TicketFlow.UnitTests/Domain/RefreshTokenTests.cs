using TicketFlow.Domain.Entities;

namespace TicketFlow.UnitTests.Domain;

public sealed class RefreshTokenTests
{
    [Fact]
    public void IsActive_ReturnsTrue_WhenTokenIsNotExpiredAndNotRevoked()
    {
        var now = new DateTime(2026, 05, 17, 10, 00, 00, DateTimeKind.Utc);
        var token = new RefreshToken(Guid.NewGuid(), Guid.NewGuid(), "token-hash", now, now.AddDays(30));

        var isActive = token.IsActive(now);

        Assert.True(isActive);
    }

    [Fact]
    public void IsActive_ReturnsFalse_WhenTokenIsExpired()
    {
        var now = new DateTime(2026, 05, 17, 10, 00, 00, DateTimeKind.Utc);
        var token = new RefreshToken(Guid.NewGuid(), Guid.NewGuid(), "token-hash", now.AddDays(-31), now.AddDays(-1));

        var isActive = token.IsActive(now);

        Assert.False(isActive);
    }

    [Fact]
    public void Revoke_MarksTokenAsInactive()
    {
        var now = new DateTime(2026, 05, 17, 10, 00, 00, DateTimeKind.Utc);
        var token = new RefreshToken(Guid.NewGuid(), Guid.NewGuid(), "token-hash", now, now.AddDays(30));

        token.Revoke(now.AddMinutes(1));

        Assert.False(token.IsActive(now.AddMinutes(2)));
    }
}
