using Microsoft.AspNetCore.SignalR;
using TicketFlow.Api.Hubs;
using TicketFlow.Application.Abstractions.Realtime;

namespace TicketFlow.Api.Realtime;

internal sealed class SignalRSeatStatusNotifier : ISeatStatusNotifier
{
    private readonly IHubContext<SeatHub> _hubContext;

    public SignalRSeatStatusNotifier(IHubContext<SeatHub> hubContext)
    {
        _hubContext = hubContext;
    }

    public Task SeatsReservedAsync(
        Guid sessionId,
        Guid reservationId,
        IReadOnlyCollection<Guid> seatIds,
        CancellationToken cancellationToken = default)
    {
        return _hubContext.Clients
            .Group(GetSessionGroupName(sessionId))
            .SendAsync(
                "SeatReserved",
                new
                {
                    sessionId,
                    reservationId,
                    seatIds
                },
                cancellationToken);
    }

    private static string GetSessionGroupName(Guid sessionId)
    {
        return $"session:{sessionId}";
    }
}
