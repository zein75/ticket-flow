using TicketFlow.Domain.Entities;
using TicketFlow.Domain.Enums;
using TicketFlow.Domain.Exceptions;

namespace TicketFlow.UnitTests.Domain;

public sealed class ReservationTests
{
    [Fact]
    public void IsExpired_ReturnsTrue_WhenExpirationTimeHasPassed()
    {
        var now = new DateTime(2026, 05, 17, 10, 00, 00, DateTimeKind.Utc);
        var reservation = CreateReservation(now.AddMinutes(-1));

        var isExpired = reservation.IsExpired(now);

        Assert.True(isExpired);
    }

    [Fact]
    public void ConvertToOrder_ChangesStatus_WhenReservationIsActiveAndNotExpired()
    {
        var now = new DateTime(2026, 05, 17, 10, 00, 00, DateTimeKind.Utc);
        var reservation = CreateReservation(now.AddMinutes(10));

        reservation.ConvertToOrder(now);

        Assert.Equal(ReservationStatus.ConvertedToOrder, reservation.Status);
    }

    [Fact]
    public void ConvertToOrder_Throws_WhenReservationIsExpired()
    {
        var now = new DateTime(2026, 05, 17, 10, 00, 00, DateTimeKind.Utc);
        var reservation = CreateReservation(now.AddMinutes(-1));

        Assert.Throws<InvalidReservationStateException>(() => reservation.ConvertToOrder(now));
    }

    private static Reservation CreateReservation(DateTime expiresAtUtc)
    {
        return new Reservation(
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            expiresAtUtc,
            [Guid.NewGuid()]);
    }
}
