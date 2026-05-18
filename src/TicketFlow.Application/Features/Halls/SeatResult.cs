namespace TicketFlow.Application.Features.Halls;

public sealed record SeatResult(Guid Id, Guid HallId, int Row, int Number);
