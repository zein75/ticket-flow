namespace TicketFlow.Api.Contracts.Payments;

public sealed record PaymentResponse(
    Guid ReservationId,
    bool Succeeded,
    string Status,
    string? ProviderPaymentId,
    string? FailureReason);
