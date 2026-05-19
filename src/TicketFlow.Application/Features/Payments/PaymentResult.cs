namespace TicketFlow.Application.Features.Payments;

public sealed record PaymentResult(
    Guid ReservationId,
    bool Succeeded,
    string Status,
    string? ProviderPaymentId,
    string? FailureReason);
