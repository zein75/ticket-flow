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
            .Group(SeatHub.GetSessionGroupName(sessionId))
            .SendAsync(
                "SeatReserved",
                new SeatReservedEvent(sessionId, reservationId, seatIds),
                cancellationToken);
    }

    public Task SeatsReleasedAsync(
        Guid sessionId,
        Guid reservationId,
        IReadOnlyCollection<Guid> seatIds,
        CancellationToken cancellationToken = default)
    {
        return _hubContext.Clients
            .Group(SeatHub.GetSessionGroupName(sessionId))
            .SendAsync(
                "SeatReleased",
                new SeatReleasedEvent(sessionId, reservationId, seatIds),
                cancellationToken);
    }

    public Task SeatsSoldAsync(
        Guid sessionId,
        Guid orderId,
        IReadOnlyCollection<Guid> seatIds,
        CancellationToken cancellationToken = default)
    {
        return _hubContext.Clients
            .Group(SeatHub.GetSessionGroupName(sessionId))
            .SendAsync(
                "SeatSold",
                new SeatSoldEvent(sessionId, orderId, seatIds),
                cancellationToken);
    }
}
