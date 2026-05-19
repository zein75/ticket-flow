using TicketFlow.Application.Abstractions.Common;
using TicketFlow.Application.Abstractions.Payments;
using TicketFlow.Application.Abstractions.Persistence;
using TicketFlow.Domain.Enums;

namespace TicketFlow.Application.Features.Payments;

public sealed class PaymentService : IPaymentService
{
    private readonly IReservationRepository _reservations;
    private readonly ISessionRepository _sessions;
    private readonly IPaymentProvider _paymentProvider;
    private readonly IClock _clock;

    public PaymentService(
        IReservationRepository reservations,
        ISessionRepository sessions,
        IPaymentProvider paymentProvider,
        IClock clock)
    {
        _reservations = reservations;
        _sessions = sessions;
        _paymentProvider = paymentProvider;
        _clock = clock;
    }

    public async Task<PaymentResult> PayReservationAsync(PayReservationCommand command, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(command.CardNumber))
        {
            throw PaymentException.Validation("Card number is required.");
        }

        var reservation = await _reservations.GetByIdWithSeatsAsync(command.ReservationId, cancellationToken)
            ?? throw PaymentException.ReservationNotFound();

        if (reservation.UserId != command.UserId)
        {
            throw PaymentException.ReservationAccessDenied();
        }

        if (reservation.Status != ReservationStatus.Active || reservation.IsExpired(_clock.UtcNow))
        {
            throw PaymentException.ReservationUnavailable();
        }

        var session = await _sessions.GetByIdAsync(reservation.SessionId, cancellationToken)
            ?? throw PaymentException.ReservationUnavailable();
        var paymentResult = await _paymentProvider.PayAsync(
            new PaymentProviderRequest(
                reservation.Id,
                reservation.UserId,
                session.Price.Amount * reservation.Seats.Count,
                session.Price.Currency,
                command.CardNumber),
            cancellationToken);

        return new PaymentResult(
            reservation.Id,
            paymentResult.Succeeded,
            paymentResult.Succeeded ? "Succeeded" : "Failed",
            paymentResult.ProviderPaymentId,
            paymentResult.FailureReason);
    }
}
