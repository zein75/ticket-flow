using Microsoft.EntityFrameworkCore;
using TicketFlow.Application.Abstractions.Persistence;
using TicketFlow.Domain.Entities;
using TicketFlow.Domain.Enums;

namespace TicketFlow.Infrastructure.Persistence.Repositories;

internal sealed class ReservationRepository : Repository<Reservation>, IReservationRepository
{
    public ReservationRepository(TicketFlowDbContext dbContext)
        : base(dbContext)
    {
    }

    public Task<Reservation?> GetByIdWithSeatsAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return DbContext.Reservations
            .Include(reservation => reservation.Seats)
            .FirstOrDefaultAsync(reservation => reservation.Id == id, cancellationToken);
    }

    public Task ReloadAsync(Reservation reservation, CancellationToken cancellationToken = default)
    {
        return DbContext.Entry(reservation).ReloadAsync(cancellationToken);
    }

    public async Task<IReadOnlyCollection<Reservation>> GetExpiredActiveAsync(DateTime utcNow, CancellationToken cancellationToken = default)
    {
        return await DbContext.Reservations
            .Include(reservation => reservation.Seats)
            .Where(reservation => reservation.Status == ReservationStatus.Active && reservation.ExpiresAtUtc <= utcNow)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyCollection<Reservation>> GetBySessionIdsWithSeatsAsync(
        IReadOnlyCollection<Guid> sessionIds,
        CancellationToken cancellationToken = default)
    {
        return await DbContext.Reservations
            .Include(reservation => reservation.Seats)
            .Where(reservation => sessionIds.Contains(reservation.SessionId))
            .ToListAsync(cancellationToken);
    }
}
