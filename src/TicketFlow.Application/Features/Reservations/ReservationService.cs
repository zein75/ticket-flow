using Microsoft.Extensions.Options;
using TicketFlow.Application.Abstractions.Common;
using TicketFlow.Application.Abstractions.Persistence;
using TicketFlow.Application.Abstractions.Realtime;
using TicketFlow.Application.Abstractions.Reservations;
using TicketFlow.Domain.Entities;

namespace TicketFlow.Application.Features.Reservations;

public sealed class ReservationService : IReservationService
{
    private readonly ISessionRepository _sessions;
    private readonly IHallRepository _halls;
    private readonly ISeatStatusRepository _seatStatuses;
    private readonly IReservationRepository _reservations;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ISeatLockStore _seatLocks;
    private readonly ISeatStatusNotifier _seatStatusNotifier;
    private readonly IClock _clock;
    private readonly ReservationOptions _options;

    public ReservationService(
        ISessionRepository sessions,
        IHallRepository halls,
        ISeatStatusRepository seatStatuses,
        IReservationRepository reservations,
        IUnitOfWork unitOfWork,
        ISeatLockStore seatLocks,
        ISeatStatusNotifier seatStatusNotifier,
        IClock clock,
        IOptions<ReservationOptions> options)
    {
        _sessions = sessions;
        _halls = halls;
        _seatStatuses = seatStatuses;
        _reservations = reservations;
        _unitOfWork = unitOfWork;
        _seatLocks = seatLocks;
        _seatStatusNotifier = seatStatusNotifier;
        _clock = clock;
        _options = options.Value;
    }

    public async Task<ReservationResult> CreateAsync(CreateReservationCommand command, CancellationToken cancellationToken = default)
    {
        var seatIds = command.SeatIds.Distinct().ToArray();
        if (seatIds.Length == 0)
        {
            throw ReservationException.Validation("Reservation must contain at least one seat.");
        }

        var session = await _sessions.GetByIdAsync(command.SessionId, cancellationToken)
            ?? throw ReservationException.SessionNotFound();
        var hall = await _halls.GetByIdWithSeatsAsync(session.HallId, cancellationToken)
            ?? throw ReservationException.SessionNotFound();
        var hallSeatIds = hall.Seats.Select(seat => seat.Id).ToHashSet();
        if (seatIds.Any(seatId => !hallSeatIds.Contains(seatId)))
        {
            throw ReservationException.SeatNotFound();
        }

        var soldSeatIds = await _seatStatuses.GetSoldSeatIdsAsync(session.Id, cancellationToken);
        var reservedSeatIds = await _seatStatuses.GetReservedSeatIdsAsync(session.Id, _clock.UtcNow, cancellationToken);
        if (seatIds.Any(seatId => soldSeatIds.Contains(seatId) || reservedSeatIds.Contains(seatId)))
        {
            throw ReservationException.SeatUnavailable();
        }

        var reservationId = Guid.NewGuid();
        var ttl = GetReservationTtl();
        var locked = await _seatLocks.TryLockSeatsAsync(session.Id, reservationId, seatIds, ttl, cancellationToken);
        if (!locked)
        {
            throw ReservationException.SeatUnavailable();
        }

        try
        {
            var reservation = new Reservation(
                reservationId,
                command.UserId,
                session.Id,
                _clock.UtcNow.Add(ttl),
                seatIds);

            _reservations.Add(reservation);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
            await _seatStatusNotifier.SeatsReservedAsync(session.Id, reservation.Id, seatIds, cancellationToken);

            return ToResult(reservation);
        }
        catch
        {
            await _seatLocks.ReleaseSeatsAsync(session.Id, reservationId, seatIds, cancellationToken);
            throw;
        }
    }

    private TimeSpan GetReservationTtl()
    {
        if (_options.TtlMinutes <= 0)
        {
            throw new InvalidOperationException("Reservation TTL must be positive.");
        }

        return TimeSpan.FromMinutes(_options.TtlMinutes);
    }

    private static ReservationResult ToResult(Reservation reservation)
    {
        return new ReservationResult(
            reservation.Id,
            reservation.UserId,
            reservation.SessionId,
            reservation.ExpiresAtUtc,
            reservation.Status.ToString(),
            reservation.Seats.Select(seat => seat.SeatId).ToList());
    }
}
