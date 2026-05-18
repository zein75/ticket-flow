namespace TicketFlow.Application.Features.SeatMaps;

public interface ISeatMapService
{
    Task<SeatMapResult> GetAsync(GetSessionSeatMapQuery query, CancellationToken cancellationToken = default);
}
