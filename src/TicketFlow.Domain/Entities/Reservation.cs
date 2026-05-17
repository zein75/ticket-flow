using TicketFlow.Domain.Abstractions;
using TicketFlow.Domain.Enums;
using TicketFlow.Domain.Exceptions;

namespace TicketFlow.Domain.Entities;

public sealed class Reservation : Entity
{
    private readonly List<ReservationSeat> _seats = [];

    public Reservation(Guid id, Guid userId, Guid sessionId, DateTime expiresAtUtc, IEnumerable<Guid> seatIds)
        : base(id)
    {
        if (userId == Guid.Empty)
        {
            throw new ArgumentException("User id cannot be empty.", nameof(userId));
        }

        if (sessionId == Guid.Empty)
        {
            throw new ArgumentException("Session id cannot be empty.", nameof(sessionId));
        }

        ArgumentNullException.ThrowIfNull(seatIds);

        var distinctSeatIds = seatIds.Distinct().ToArray();
        if (distinctSeatIds.Length == 0)
        {
            throw new ArgumentException("Reservation must contain at least one seat.", nameof(seatIds));
        }

        UserId = userId;
        SessionId = sessionId;
        ExpiresAtUtc = expiresAtUtc;
        Status = ReservationStatus.Active;
        _seats.AddRange(distinctSeatIds.Select(seatId => new ReservationSeat(Id, seatId)));
    }

    public Guid UserId { get; }

    public Guid SessionId { get; }

    public DateTime ExpiresAtUtc { get; }

    public ReservationStatus Status { get; private set; }

    public IReadOnlyCollection<ReservationSeat> Seats => _seats.AsReadOnly();

    public bool IsExpired(DateTime utcNow) => ExpiresAtUtc <= utcNow;

    public void Expire(DateTime utcNow)
    {
        if (Status != ReservationStatus.Active)
        {
            return;
        }

        if (!IsExpired(utcNow))
        {
            throw new InvalidReservationStateException("Cannot expire a reservation before its expiration time.");
        }

        Status = ReservationStatus.Expired;
    }

    public void Cancel()
    {
        EnsureActive("Only active reservations can be cancelled.");

        Status = ReservationStatus.Cancelled;
    }

    public void ConvertToOrder(DateTime utcNow)
    {
        EnsureActive("Only active reservations can be converted to an order.");

        if (IsExpired(utcNow))
        {
            throw new InvalidReservationStateException("Expired reservation cannot be converted to an order.");
        }

        Status = ReservationStatus.ConvertedToOrder;
    }

    private void EnsureActive(string message)
    {
        if (Status != ReservationStatus.Active)
        {
            throw new InvalidReservationStateException(message);
        }
    }
}
