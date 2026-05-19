namespace TicketFlow.Application.Features.Payments;

public sealed record PaymentResult(
    Guid ReservationId,
    Guid? OrderId,
    bool Succeeded,
    string Status,
    string? ProviderPaymentId,
    string? FailureReason,
    IReadOnlyCollection<Guid> TicketIds);
