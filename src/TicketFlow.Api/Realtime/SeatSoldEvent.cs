namespace TicketFlow.Api.Realtime;

public sealed record SeatSoldEvent(Guid SessionId, Guid OrderId, IReadOnlyCollection<Guid> SeatIds);
