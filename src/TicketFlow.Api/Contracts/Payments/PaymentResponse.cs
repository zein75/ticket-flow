namespace TicketFlow.Api.Contracts.Payments;

public sealed record PaymentResponse(
    Guid ReservationId,
    Guid? OrderId,
    bool Succeeded,
    string Status,
    string? ProviderPaymentId,
    string? FailureReason,
    IReadOnlyCollection<Guid> TicketIds);
