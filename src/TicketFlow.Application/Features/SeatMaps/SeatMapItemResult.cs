namespace TicketFlow.Application.Features.SeatMaps;

public sealed record SeatMapItemResult(Guid SeatId, int Row, int Number, SeatMapStatus Status);
