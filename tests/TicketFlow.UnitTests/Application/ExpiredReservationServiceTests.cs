using TicketFlow.Application.Abstractions.Common;
using TicketFlow.Application.Abstractions.Persistence;
using TicketFlow.Application.Abstractions.Realtime;
using TicketFlow.Application.Features.ReservationExpirations;
using TicketFlow.Domain.Entities;
using TicketFlow.Domain.Enums;

namespace TicketFlow.UnitTests.Application;

public sealed class ExpiredReservationServiceTests
{
    [Fact]
    public async Task ExpireAsync_ExpiresActiveOutdatedReservations_AndSendsSeatReleasedEvents()
    {
        var now = new DateTime(2026, 05, 19, 10, 00, 00, DateTimeKind.Utc);
        var expiredReservation = new Reservation(
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            now.AddSeconds(-1),
            [Guid.NewGuid(), Guid.NewGuid()]);
        var repository = new FakeReservationRepository([expiredReservation]);
        var unitOfWork = new FakeUnitOfWork();
        var notifier = new FakeSeatStatusNotifier();
        var service = new ExpiredReservationService(repository, unitOfWork, notifier, new FakeClock(now));

        var result = await service.ExpireAsync();

        Assert.Equal(ReservationStatus.Expired, expiredReservation.Status);
        Assert.Equal(1, result.Count);
        Assert.Equal(1, unitOfWork.SaveChangesCalls);
        var notification = Assert.Single(notifier.Released);
        Assert.Equal(expiredReservation.Id, notification.ReservationId);
        Assert.Equal(expiredReservation.SessionId, notification.SessionId);
        Assert.Equal(expiredReservation.Seats.Select(seat => seat.SeatId), notification.SeatIds);
    }

    [Fact]
    public async Task ExpireAsync_DoesNotSaveOrNotify_WhenNoExpiredReservationsExist()
    {
        var now = new DateTime(2026, 05, 19, 10, 00, 00, DateTimeKind.Utc);
        var repository = new FakeReservationRepository([]);
        var unitOfWork = new FakeUnitOfWork();
        var notifier = new FakeSeatStatusNotifier();
        var service = new ExpiredReservationService(repository, unitOfWork, notifier, new FakeClock(now));

        var result = await service.ExpireAsync();

        Assert.Equal(0, result.Count);
        Assert.Equal(0, unitOfWork.SaveChangesCalls);
        Assert.Empty(notifier.Released);
    }

    private sealed class FakeReservationRepository : IReservationRepository
    {
        private readonly IReadOnlyCollection<Reservation> _expiredReservations;

        public FakeReservationRepository(IReadOnlyCollection<Reservation> expiredReservations)
        {
            _expiredReservations = expiredReservations;
        }

        public Task<Reservation?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
            Task.FromResult<Reservation?>(null);

        public void Add(Reservation entity)
        {
        }

        public void Remove(Reservation entity)
        {
        }

        public Task<Reservation?> GetByIdWithSeatsAsync(Guid id, CancellationToken cancellationToken = default) =>
            Task.FromResult<Reservation?>(null);

        public Task ReloadAsync(Reservation reservation, CancellationToken cancellationToken = default) =>
            Task.CompletedTask;

        public Task<IReadOnlyCollection<Reservation>> GetExpiredActiveAsync(DateTime utcNow, CancellationToken cancellationToken = default) =>
            Task.FromResult(_expiredReservations);

        public Task<IReadOnlyCollection<Reservation>> GetBySessionIdsWithSeatsAsync(
            IReadOnlyCollection<Guid> sessionIds,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyCollection<Reservation>>([]);
    }

    private sealed class FakeUnitOfWork : IUnitOfWork
    {
        public int SaveChangesCalls { get; private set; }

        public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            SaveChangesCalls++;
            return Task.FromResult(1);
        }

        public Task ExecuteInTransactionAsync(Func<CancellationToken, Task> operation, CancellationToken cancellationToken = default) =>
            operation(cancellationToken);
    }

    private sealed class FakeSeatStatusNotifier : ISeatStatusNotifier
    {
        public List<SeatNotification> Released { get; } = [];

        public Task SeatsReservedAsync(
            Guid sessionId,
            Guid reservationId,
            IReadOnlyCollection<Guid> seatIds,
            CancellationToken cancellationToken = default) =>
            Task.CompletedTask;

        public Task SeatsReleasedAsync(
            Guid sessionId,
            Guid reservationId,
            IReadOnlyCollection<Guid> seatIds,
            CancellationToken cancellationToken = default)
        {
            Released.Add(new SeatNotification(sessionId, reservationId, seatIds));
            return Task.CompletedTask;
        }

        public Task SeatsSoldAsync(
            Guid sessionId,
            Guid orderId,
            IReadOnlyCollection<Guid> seatIds,
            CancellationToken cancellationToken = default) =>
            Task.CompletedTask;
    }

    private sealed class FakeClock : IClock
    {
        public FakeClock(DateTime utcNow)
        {
            UtcNow = utcNow;
        }

        public DateTime UtcNow { get; }
    }

    private sealed record SeatNotification(
        Guid SessionId,
        Guid ReservationId,
        IReadOnlyCollection<Guid> SeatIds);
}
