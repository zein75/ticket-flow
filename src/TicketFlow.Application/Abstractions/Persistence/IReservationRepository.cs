using TicketFlow.Domain.Entities;

namespace TicketFlow.Application.Abstractions.Persistence;

public interface IReservationRepository : IRepository<Reservation>
{
    Task<Reservation?> GetByIdWithSeatsAsync(Guid id, CancellationToken cancellationToken = default);

    Task ReloadAsync(Reservation reservation, CancellationToken cancellationToken = default);

    Task<IReadOnlyCollection<Reservation>> GetExpiredActiveAsync(DateTime utcNow, CancellationToken cancellationToken = default);

    Task<IReadOnlyCollection<Reservation>> GetBySessionIdsWithSeatsAsync(
        IReadOnlyCollection<Guid> sessionIds,
        CancellationToken cancellationToken = default);
}
