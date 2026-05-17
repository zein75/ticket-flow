using Microsoft.EntityFrameworkCore;
using TicketFlow.Domain.Entities;
using TicketFlow.Infrastructure.Persistence;

namespace TicketFlow.IntegrationTests.Persistence;

public sealed class TicketFlowDbContextModelTests
{
    [Fact]
    public void Model_ShouldContainHallLayoutColumns()
    {
        using var dbContext = CreateDbContext();
        var hall = dbContext.Model.FindEntityType(typeof(Hall));

        Assert.NotNull(hall);
        Assert.NotNull(hall.FindProperty(nameof(Hall.Rows)));
        Assert.NotNull(hall.FindProperty(nameof(Hall.SeatsPerRow)));
    }

    [Fact]
    public void Model_ShouldProtectSoldSeatsWithUniqueTicketIndex()
    {
        using var dbContext = CreateDbContext();
        var ticket = dbContext.Model.FindEntityType(typeof(Ticket));

        var index = ticket?.GetIndexes().SingleOrDefault(index =>
            index.Properties.Select(property => property.Name).SequenceEqual([nameof(Ticket.SessionId), nameof(Ticket.SeatId)]));

        Assert.NotNull(index);
        Assert.True(index.IsUnique);
    }

    [Fact]
    public void Model_ShouldHaveRefreshTokenConcurrencyToken()
    {
        using var dbContext = CreateDbContext();
        var refreshToken = dbContext.Model.FindEntityType(typeof(RefreshToken));
        var revokedAt = refreshToken?.FindProperty(nameof(RefreshToken.RevokedAtUtc));

        Assert.NotNull(revokedAt);
        Assert.True(revokedAt.IsConcurrencyToken);
    }

    [Fact]
    public void Model_ShouldHaveForeignKeysForCorePurchaseFlow()
    {
        using var dbContext = CreateDbContext();

        AssertForeignKey<Order>(dbContext, nameof(Order.UserId));
        AssertForeignKey<Order>(dbContext, nameof(Order.SessionId));
        AssertForeignKey<Order>(dbContext, nameof(Order.ReservationId));
        AssertForeignKey<Payment>(dbContext, nameof(Payment.OrderId));
        AssertForeignKey<Ticket>(dbContext, nameof(Ticket.SessionId));
        AssertForeignKey<Ticket>(dbContext, nameof(Ticket.SeatId));
    }

    private static void AssertForeignKey<TEntity>(TicketFlowDbContext dbContext, string propertyName)
    {
        var entityType = dbContext.Model.FindEntityType(typeof(TEntity));
        var hasForeignKey = entityType?.GetForeignKeys()
            .Any(foreignKey => foreignKey.Properties.Any(property => property.Name == propertyName));

        Assert.True(hasForeignKey, $"{typeof(TEntity).Name}.{propertyName} should be protected by a foreign key.");
    }

    private static TicketFlowDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<TicketFlowDbContext>()
            .UseNpgsql("Host=localhost;Database=ticketflow;Username=ticketflow;Password=ticketflow_password")
            .Options;

        return new TicketFlowDbContext(options);
    }
}
