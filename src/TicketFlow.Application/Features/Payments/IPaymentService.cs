namespace TicketFlow.Application.Features.Payments;

public interface IPaymentService
{
    Task<PaymentResult> PayReservationAsync(PayReservationCommand command, CancellationToken cancellationToken = default);
}
