namespace TicketFlow.Domain.Entities;

public sealed class ReservationSeat
{
    private ReservationSeat()
    {
    }

    public ReservationSeat(Guid reservationId, Guid seatId)
    {
        ReservationId = reservationId == Guid.Empty
            ? throw new ArgumentException("Reservation id cannot be empty.", nameof(reservationId))
            : reservationId;
        SeatId = seatId == Guid.Empty ? throw new ArgumentException("Seat id cannot be empty.", nameof(seatId)) : seatId;
    }

    public Guid ReservationId { get; }

    public Guid SeatId { get; }
}
