namespace TicketFlow.Api.Realtime;

public sealed record SeatReservedEvent(Guid SessionId, Guid ReservationId, IReadOnlyCollection<Guid> SeatIds);
