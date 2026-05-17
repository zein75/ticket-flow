using TicketFlow.Domain.Entities;

namespace TicketFlow.Application.Abstractions.Persistence;

public interface IEventRepository : IRepository<Event>
{
    Task<IReadOnlyCollection<Event>> GetPublishedAsync(CancellationToken cancellationToken = default);
}
