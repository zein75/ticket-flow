namespace TicketFlow.Application.Features.ReservationExpirations;

public sealed record ExpiredReservationResult(
    Guid ReservationId,
    Guid SessionId,
    IReadOnlyCollection<Guid> SeatIds);
