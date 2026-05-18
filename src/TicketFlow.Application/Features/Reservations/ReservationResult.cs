namespace TicketFlow.Application.Features.Reservations;

public sealed record ReservationResult(
    Guid Id,
    Guid UserId,
    Guid SessionId,
    DateTime ExpiresAtUtc,
    string Status,
    IReadOnlyCollection<Guid> SeatIds);
