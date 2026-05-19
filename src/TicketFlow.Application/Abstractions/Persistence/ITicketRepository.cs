using TicketFlow.Domain.Entities;

namespace TicketFlow.Application.Abstractions.Persistence;

public interface ITicketRepository : IRepository<Ticket>
{
    Task<Ticket?> GetByIdWithOrderAsync(Guid id, CancellationToken cancellationToken = default);

    Task<IReadOnlyCollection<Ticket>> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken = default);
}
