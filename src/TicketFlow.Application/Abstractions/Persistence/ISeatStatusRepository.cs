namespace TicketFlow.Application.Abstractions.Persistence;

public interface ISeatStatusRepository
{
    Task<IReadOnlySet<Guid>> GetSoldSeatIdsAsync(Guid sessionId, CancellationToken cancellationToken = default);

    Task<IReadOnlySet<Guid>> GetReservedSeatIdsAsync(
        Guid sessionId,
        DateTime utcNow,
        CancellationToken cancellationToken = default);
}
