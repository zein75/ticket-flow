using TicketFlow.Domain.Entities;

namespace TicketFlow.Application.Abstractions.Persistence;

public interface ISessionRepository : IRepository<Session>
{
    Task<IReadOnlyCollection<Session>> GetByEventIdAsync(Guid eventId, CancellationToken cancellationToken = default);

    Task<IReadOnlyCollection<Session>> GetByEventIdsAsync(
        IReadOnlyCollection<Guid> eventIds,
        CancellationToken cancellationToken = default);
}
