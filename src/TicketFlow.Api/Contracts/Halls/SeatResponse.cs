namespace TicketFlow.Api.Contracts.Halls;

public sealed record SeatResponse(Guid Id, Guid HallId, int Row, int Number);
