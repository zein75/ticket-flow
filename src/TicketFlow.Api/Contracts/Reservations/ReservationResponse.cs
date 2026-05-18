namespace TicketFlow.Api.Contracts.Reservations;

public sealed record ReservationResponse(
    Guid Id,
    Guid UserId,
    Guid SessionId,
    DateTime ExpiresAtUtc,
    string Status,
    IReadOnlyCollection<Guid> SeatIds);
