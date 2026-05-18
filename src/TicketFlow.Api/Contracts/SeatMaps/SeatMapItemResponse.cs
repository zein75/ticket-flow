namespace TicketFlow.Api.Contracts.SeatMaps;

public sealed record SeatMapItemResponse(Guid SeatId, int Row, int Number, string Status);
