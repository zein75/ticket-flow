namespace TicketFlow.Application.Features.Admin;

public sealed record AdminOrderResult(
    Guid Id,
    Guid UserId,
    Guid SessionId,
    Guid ReservationId,
    decimal TotalAmount,
    string Currency,
    string Status,
    IReadOnlyCollection<Guid> TicketIds);
