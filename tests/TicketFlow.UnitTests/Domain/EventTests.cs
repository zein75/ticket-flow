using TicketFlow.Domain.Entities;
using TicketFlow.Domain.Enums;

namespace TicketFlow.UnitTests.Domain;

public sealed class EventTests
{
    [Fact]
    public void ChangeStatus_UpdatesStatus_WhenStatusIsValid()
    {
        var ticketEvent = new Event(Guid.NewGuid(), "Concert", "Live concert", Guid.NewGuid());

        ticketEvent.ChangeStatus(EventStatus.Published);

        Assert.Equal(EventStatus.Published, ticketEvent.Status);
    }

    [Fact]
    public void ChangeStatus_Throws_WhenStatusIsInvalid()
    {
        var ticketEvent = new Event(Guid.NewGuid(), "Concert", "Live concert", Guid.NewGuid());

        Assert.Throws<ArgumentOutOfRangeException>(() => ticketEvent.ChangeStatus((EventStatus)999));
    }
}
