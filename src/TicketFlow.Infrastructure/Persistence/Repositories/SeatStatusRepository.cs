using Microsoft.EntityFrameworkCore;
using TicketFlow.Application.Abstractions.Persistence;
using TicketFlow.Domain.Enums;

namespace TicketFlow.Infrastructure.Persistence.Repositories;

internal sealed class SeatStatusRepository : ISeatStatusRepository
{
    private readonly TicketFlowDbContext _dbContext;

    public SeatStatusRepository(TicketFlowDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<IReadOnlySet<Guid>> GetSoldSeatIdsAsync(Guid sessionId, CancellationToken cancellationToken = default)
    {
        var seatIds = await _dbContext.Tickets
            .Where(ticket => ticket.SessionId == sessionId && ticket.Status == TicketStatus.Active)
            .Select(ticket => ticket.SeatId)
            .ToListAsync(cancellationToken);

        return seatIds.ToHashSet();
    }

    public async Task<IReadOnlySet<Guid>> GetReservedSeatIdsAsync(
        Guid sessionId,
        DateTime utcNow,
        CancellationToken cancellationToken = default)
    {
        var seatIds = await _dbContext.Reservations
            .Where(reservation =>
                reservation.SessionId == sessionId &&
                reservation.Status == ReservationStatus.Active &&
                reservation.ExpiresAtUtc > utcNow)
            .SelectMany(reservation => reservation.Seats.Select(seat => seat.SeatId))
            .ToListAsync(cancellationToken);

        return seatIds.ToHashSet();
    }
}
