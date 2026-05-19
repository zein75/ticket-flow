using TicketFlow.Application.Abstractions.Payments;

namespace TicketFlow.Infrastructure.Payments;

internal sealed class MockPaymentProvider : IPaymentProvider
{
    public Task<PaymentProviderResult> PayAsync(PaymentProviderRequest request, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(request.CardNumber))
        {
            return Task.FromResult(new PaymentProviderResult(false, null, "Card number is required."));
        }

        var normalizedCardNumber = request.CardNumber.Replace(" ", string.Empty, StringComparison.Ordinal);
        if (normalizedCardNumber.EndsWith("0000", StringComparison.Ordinal))
        {
            return Task.FromResult(new PaymentProviderResult(false, null, "Payment was declined by mock provider."));
        }

        return Task.FromResult(new PaymentProviderResult(true, $"mock_{Guid.NewGuid():N}", null));
    }
}
