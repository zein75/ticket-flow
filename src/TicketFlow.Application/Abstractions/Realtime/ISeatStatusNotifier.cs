namespace TicketFlow.Application.Abstractions.Realtime;

public interface ISeatStatusNotifier
{
    Task SeatsReservedAsync(
        Guid sessionId,
        Guid reservationId,
        IReadOnlyCollection<Guid> seatIds,
        CancellationToken cancellationToken = default);

    Task SeatsReleasedAsync(
        Guid sessionId,
        Guid reservationId,
        IReadOnlyCollection<Guid> seatIds,
        CancellationToken cancellationToken = default);

    Task SeatsSoldAsync(
        Guid sessionId,
        Guid orderId,
        IReadOnlyCollection<Guid> seatIds,
        CancellationToken cancellationToken = default);
}
