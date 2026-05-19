using Microsoft.EntityFrameworkCore;
using Npgsql;
using TicketFlow.Application.Abstractions.Persistence;

namespace TicketFlow.Infrastructure.Persistence;

internal sealed class UnitOfWork : IUnitOfWork
{
    private readonly TicketFlowDbContext _dbContext;

    public UnitOfWork(TicketFlowDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            return await _dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException exception)
        {
            throw new PersistenceConcurrencyException("A concurrency conflict occurred while saving changes.", exception);
        }
        catch (DbUpdateException exception) when (IsUniqueViolation(exception))
        {
            throw new PersistenceConflictException("A persistence conflict occurred while saving changes.", exception);
        }
    }

    public async Task ExecuteInTransactionAsync(
        Func<CancellationToken, Task> operation,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(operation);

        await using var transaction = await _dbContext.Database.BeginTransactionAsync(cancellationToken);

        try
        {
            await operation(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
        }
        catch
        {
            await transaction.RollbackAsync(cancellationToken);
            throw;
        }
    }

    private static bool IsUniqueViolation(DbUpdateException exception) =>
        exception.InnerException is PostgresException postgresException
        && postgresException.SqlState == PostgresErrorCodes.UniqueViolation;
}
