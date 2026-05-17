using TicketFlow.Application.Abstractions.Persistence;

namespace TicketFlow.Infrastructure.Persistence;

internal sealed class UnitOfWork : IUnitOfWork
{
    private readonly TicketFlowDbContext _dbContext;

    public UnitOfWork(TicketFlowDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            return _dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (Microsoft.EntityFrameworkCore.DbUpdateConcurrencyException exception)
        {
            throw new PersistenceConcurrencyException("A concurrency conflict occurred while saving changes.", exception);
        }
    }
}
