namespace TicketFlow.Application.Features.Tickets;

public sealed record TicketResult(
    Guid Id,
    Guid OrderId,
    Guid SessionId,
    Guid SeatId,
    string Status);
