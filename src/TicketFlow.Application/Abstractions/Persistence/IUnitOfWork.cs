namespace TicketFlow.Application.Abstractions.Persistence;

public interface IUnitOfWork
{
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);

    Task ExecuteInTransactionAsync(
        Func<CancellationToken, Task> operation,
        CancellationToken cancellationToken = default);
}

public sealed class PersistenceConcurrencyException : Exception
{
    public PersistenceConcurrencyException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}

public sealed class PersistenceConflictException : Exception
{
    public PersistenceConflictException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
