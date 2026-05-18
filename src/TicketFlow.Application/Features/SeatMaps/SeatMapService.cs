using TicketFlow.Application.Abstractions.Common;
using TicketFlow.Application.Abstractions.Persistence;
using TicketFlow.Domain.Entities;
using TicketFlow.Domain.Enums;

namespace TicketFlow.Application.Features.SeatMaps;

public sealed class SeatMapService : ISeatMapService
{
    private readonly ISessionRepository _sessions;
    private readonly IEventRepository _events;
    private readonly IHallRepository _halls;
    private readonly ISeatStatusRepository _seatStatuses;
    private readonly IClock _clock;

    public SeatMapService(
        ISessionRepository sessions,
        IEventRepository events,
        IHallRepository halls,
        ISeatStatusRepository seatStatuses,
        IClock clock)
    {
        _sessions = sessions;
        _events = events;
        _halls = halls;
        _seatStatuses = seatStatuses;
        _clock = clock;
    }

    public async Task<SeatMapResult> GetAsync(GetSessionSeatMapQuery query, CancellationToken cancellationToken = default)
    {
        var session = await _sessions.GetByIdAsync(query.SessionId, cancellationToken)
            ?? throw SeatMapException.SessionNotFound();
        var ticketEvent = await _events.GetByIdAsync(session.EventId, cancellationToken)
            ?? throw SeatMapException.SessionNotFound();

        if (!CanView(ticketEvent, query.UserId, query.Role))
        {
            throw SeatMapException.SessionNotFound();
        }

        var hall = await _halls.GetByIdWithSeatsAsync(session.HallId, cancellationToken)
            ?? throw SeatMapException.SessionNotFound();
        var soldSeatIds = await _seatStatuses.GetSoldSeatIdsAsync(session.Id, cancellationToken);
        var reservedSeatIds = await _seatStatuses.GetReservedSeatIdsAsync(session.Id, _clock.UtcNow, cancellationToken);

        var seats = hall.Seats
            .OrderBy(seat => seat.Row)
            .ThenBy(seat => seat.Number)
            .Select(seat => new SeatMapItemResult(
                seat.Id,
                seat.Row,
                seat.Number,
                ResolveStatus(seat.Id, soldSeatIds, reservedSeatIds)))
            .ToList();

        return new SeatMapResult(session.Id, seats);
    }

    private static SeatMapStatus ResolveStatus(Guid seatId, IReadOnlySet<Guid> soldSeatIds, IReadOnlySet<Guid> reservedSeatIds)
    {
        if (soldSeatIds.Contains(seatId))
        {
            return SeatMapStatus.Sold;
        }

        return reservedSeatIds.Contains(seatId) ? SeatMapStatus.Reserved : SeatMapStatus.Available;
    }

    private static bool CanView(Event ticketEvent, Guid? userId, UserRole? role)
    {
        if (ticketEvent.Status == EventStatus.Published || role == UserRole.Admin)
        {
            return true;
        }

        return role == UserRole.Organizer && userId == ticketEvent.OrganizerId;
    }
}
