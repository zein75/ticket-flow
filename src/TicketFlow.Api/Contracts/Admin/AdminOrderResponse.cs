namespace TicketFlow.Api.Contracts.Admin;

public sealed record AdminOrderResponse(
    Guid Id,
    Guid UserId,
    Guid SessionId,
    Guid ReservationId,
    decimal TotalAmount,
    string Currency,
    string Status,
    IReadOnlyCollection<Guid> TicketIds);
