using Microsoft.Extensions.Logging;
using TicketFlow.Application.Abstractions.Common;
using TicketFlow.Application.Abstractions.Persistence;
using TicketFlow.Application.Abstractions.Realtime;

namespace TicketFlow.Application.Features.ReservationExpirations;

public sealed class ExpiredReservationService : IExpiredReservationService
{
    private readonly IReservationRepository _reservations;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ISeatStatusNotifier _seatStatusNotifier;
    private readonly IClock _clock;
    private readonly ILogger<ExpiredReservationService> _logger;

    public ExpiredReservationService(
        IReservationRepository reservations,
        IUnitOfWork unitOfWork,
        ISeatStatusNotifier seatStatusNotifier,
        IClock clock,
        ILogger<ExpiredReservationService> logger)
    {
        _reservations = reservations;
        _unitOfWork = unitOfWork;
        _seatStatusNotifier = seatStatusNotifier;
        _clock = clock;
        _logger = logger;
    }

    public async Task<ExpireReservationsResult> ExpireAsync(CancellationToken cancellationToken = default)
    {
        var utcNow = _clock.UtcNow;
        var expiredReservations = await _reservations.GetExpiredActiveAsync(utcNow, cancellationToken);
        if (expiredReservations.Count == 0)
        {
            return new ExpireReservationsResult([]);
        }

        var results = new List<ExpiredReservationResult>(expiredReservations.Count);
        foreach (var reservation in expiredReservations)
        {
            reservation.Expire(utcNow);
            results.Add(new ExpiredReservationResult(
                reservation.Id,
                reservation.SessionId,
                reservation.Seats.Select(seat => seat.SeatId).ToArray()));
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        foreach (var result in results)
        {
            _logger.LogInformation(
                "ReservationExpired ReservationId={ReservationId} SessionId={SessionId} SeatCount={SeatCount}",
                result.ReservationId,
                result.SessionId,
                result.SeatIds.Count);
            await _seatStatusNotifier.SeatsReleasedAsync(
                result.SessionId,
                result.ReservationId,
                result.SeatIds,
                cancellationToken);
        }

        return new ExpireReservationsResult(results);
    }
}
