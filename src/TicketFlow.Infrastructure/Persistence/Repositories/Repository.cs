using Microsoft.EntityFrameworkCore;
using TicketFlow.Application.Abstractions.Persistence;
using TicketFlow.Domain.Abstractions;

namespace TicketFlow.Infrastructure.Persistence.Repositories;

internal class Repository<TEntity> : IRepository<TEntity>
    where TEntity : Entity
{
    protected Repository(TicketFlowDbContext dbContext)
    {
        DbContext = dbContext;
    }

    protected TicketFlowDbContext DbContext { get; }

    public virtual Task<TEntity?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return DbContext.Set<TEntity>().FirstOrDefaultAsync(entity => entity.Id == id, cancellationToken);
    }

    public void Add(TEntity entity)
    {
        DbContext.Set<TEntity>().Add(entity);
    }

    public void Remove(TEntity entity)
    {
        DbContext.Set<TEntity>().Remove(entity);
    }
}
