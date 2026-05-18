namespace TicketFlow.Application.Features.Reservations;

public sealed class ReservationException : Exception
{
    private ReservationException(string code, string message)
        : base(message)
    {
        Code = code;
    }

    public string Code { get; }

    public static ReservationException SessionNotFound() =>
        new("session_not_found", "Session was not found.");

    public static ReservationException SeatNotFound() =>
        new("seat_not_found", "One or more selected seats were not found in the session hall.");

    public static ReservationException SeatUnavailable() =>
        new("seat_unavailable", "One or more selected seats are unavailable.");

    public static ReservationException Validation(string message) =>
        new("reservation_validation_failed", message);
}
