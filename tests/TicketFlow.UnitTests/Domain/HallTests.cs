using TicketFlow.Domain.Entities;

namespace TicketFlow.UnitTests.Domain;

public sealed class HallTests
{
    [Fact]
    public void Constructor_GeneratesSeats_ForLayout()
    {
        var hall = new Hall(Guid.NewGuid(), "Main hall", 2, 3);

        Assert.Equal(6, hall.Seats.Count);
        Assert.Contains(hall.Seats, seat => seat.Row == 1 && seat.Number == 1);
        Assert.Contains(hall.Seats, seat => seat.Row == 2 && seat.Number == 3);
    }

    [Theory]
    [InlineData(0, 1)]
    [InlineData(1, 0)]
    public void Constructor_Throws_WhenLayoutIsInvalid(int rows, int seatsPerRow)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new Hall(Guid.NewGuid(), "Main hall", rows, seatsPerRow));
    }
}
