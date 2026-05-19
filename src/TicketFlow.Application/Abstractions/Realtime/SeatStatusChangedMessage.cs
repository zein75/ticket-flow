namespace TicketFlow.Application.Abstractions.Realtime;

public sealed record SeatStatusChangedMessage(
    SeatStatusChangeType Type,
    Guid SessionId,
    Guid ReservationId,
    Guid? OrderId,
    IReadOnlyCollection<Guid> SeatIds);
