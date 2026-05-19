namespace TicketFlow.Api.Contracts.Tickets;

public sealed record TicketResponse(
    Guid Id,
    Guid OrderId,
    Guid SessionId,
    Guid SeatId,
    string Status);
