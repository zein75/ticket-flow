namespace TicketFlow.Application.Abstractions.Payments;

public interface IPaymentProvider
{
    Task<PaymentProviderResult> PayAsync(PaymentProviderRequest request, CancellationToken cancellationToken = default);
}
