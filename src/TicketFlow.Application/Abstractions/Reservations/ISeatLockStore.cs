namespace TicketFlow.Application.Abstractions.Reservations;

public interface ISeatLockStore
{
    Task<bool> TryLockSeatsAsync(
        Guid sessionId,
        Guid reservationId,
        IReadOnlyCollection<Guid> seatIds,
        TimeSpan ttl,
        CancellationToken cancellationToken = default);

    Task ReleaseSeatsAsync(
        Guid sessionId,
        Guid reservationId,
        IReadOnlyCollection<Guid> seatIds,
        CancellationToken cancellationToken = default);
}
