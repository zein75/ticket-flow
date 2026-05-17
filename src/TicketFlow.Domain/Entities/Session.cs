using TicketFlow.Domain.Abstractions;
using TicketFlow.Domain.ValueObjects;

namespace TicketFlow.Domain.Entities;

public sealed class Session : Entity
{
    public Session(Guid id, Guid eventId, Guid hallId, DateTime startsAtUtc, DateTime endsAtUtc, Money price)
        : base(id)
    {
        if (eventId == Guid.Empty)
        {
            throw new ArgumentException("Event id cannot be empty.", nameof(eventId));
        }

        if (hallId == Guid.Empty)
        {
            throw new ArgumentException("Hall id cannot be empty.", nameof(hallId));
        }

        if (endsAtUtc <= startsAtUtc)
        {
            throw new ArgumentException("Session end time must be later than start time.", nameof(endsAtUtc));
        }

        ArgumentNullException.ThrowIfNull(price);

        EventId = eventId;
        HallId = hallId;
        StartsAtUtc = startsAtUtc;
        EndsAtUtc = endsAtUtc;
        Price = price;
    }

    public Guid EventId { get; }

    public Guid HallId { get; }

    public DateTime StartsAtUtc { get; private set; }

    public DateTime EndsAtUtc { get; private set; }

    public Money Price { get; private set; }
}
