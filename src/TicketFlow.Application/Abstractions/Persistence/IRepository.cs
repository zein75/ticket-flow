using TicketFlow.Domain.Abstractions;

namespace TicketFlow.Application.Abstractions.Persistence;

public interface IRepository<TEntity>
    where TEntity : Entity
{
    Task<TEntity?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    void Add(TEntity entity);

    void Remove(TEntity entity);
}
