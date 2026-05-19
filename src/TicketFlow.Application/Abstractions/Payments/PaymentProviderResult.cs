namespace TicketFlow.Application.Abstractions.Payments;

public sealed record PaymentProviderResult(bool Succeeded, string? ProviderPaymentId, string? FailureReason);
