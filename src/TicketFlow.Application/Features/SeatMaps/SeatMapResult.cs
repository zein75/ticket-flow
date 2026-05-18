namespace TicketFlow.Application.Features.SeatMaps;

public sealed record SeatMapResult(Guid SessionId, IReadOnlyCollection<SeatMapItemResult> Seats);
