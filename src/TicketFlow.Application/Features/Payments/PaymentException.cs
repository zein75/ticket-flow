namespace TicketFlow.Application.Features.Payments;

public sealed class PaymentException : Exception
{
    private PaymentException(string code, string message)
        : base(message)
    {
        Code = code;
    }

    public string Code { get; }

    public static PaymentException ReservationNotFound() =>
        new("reservation_not_found", "Reservation was not found.");

    public static PaymentException ReservationAccessDenied() =>
        new("reservation_access_denied", "Reservation does not belong to the current user.");

    public static PaymentException ReservationUnavailable() =>
        new("reservation_unavailable", "Only active non-expired reservations can be paid.");

    public static PaymentException SeatsUnavailable() =>
        new("seats_unavailable", "One or more reserved seats are no longer available.");

    public static PaymentException Validation(string message) =>
        new("payment_validation_failed", message);
}
