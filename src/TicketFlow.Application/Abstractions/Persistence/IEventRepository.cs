using TicketFlow.Domain.Entities;

namespace TicketFlow.Application.Abstractions.Persistence;

public interface IEventRepository : IRepository<Event>
{
    Task<IReadOnlyCollection<Event>> GetAllAsync(CancellationToken cancellationToken = default);

    Task<IReadOnlyCollection<Event>> GetByOrganizerIdAsync(Guid organizerId, CancellationToken cancellationToken = default);

    Task<IReadOnlyCollection<Event>> GetPublishedAsync(CancellationToken cancellationToken = default);
}
