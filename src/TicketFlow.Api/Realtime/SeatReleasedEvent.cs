namespace TicketFlow.Api.Realtime;

public sealed record SeatReleasedEvent(Guid SessionId, Guid ReservationId, IReadOnlyCollection<Guid> SeatIds);
