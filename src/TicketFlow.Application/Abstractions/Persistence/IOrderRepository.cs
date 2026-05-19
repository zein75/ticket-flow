using TicketFlow.Domain.Entities;

namespace TicketFlow.Application.Abstractions.Persistence;

public interface IOrderRepository : IRepository<Order>
{
    Task<IReadOnlyCollection<Order>> GetAllWithTicketsAsync(CancellationToken cancellationToken = default);

    Task<IReadOnlyCollection<Order>> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken = default);

    Task<IReadOnlyCollection<Order>> GetPaidBySessionIdsWithTicketsAsync(
        IReadOnlyCollection<Guid> sessionIds,
        CancellationToken cancellationToken = default);
}
