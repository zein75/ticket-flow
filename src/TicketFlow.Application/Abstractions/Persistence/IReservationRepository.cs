using TicketFlow.Domain.Entities;

namespace TicketFlow.Application.Abstractions.Persistence;

public interface IReservationRepository : IRepository<Reservation>
{
    Task<IReadOnlyCollection<Reservation>> GetExpiredActiveAsync(DateTime utcNow, CancellationToken cancellationToken = default);
}
