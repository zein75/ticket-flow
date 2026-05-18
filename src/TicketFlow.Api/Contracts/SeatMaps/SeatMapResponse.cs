namespace TicketFlow.Api.Contracts.SeatMaps;

public sealed record SeatMapResponse(Guid SessionId, IReadOnlyCollection<SeatMapItemResponse> Seats);
