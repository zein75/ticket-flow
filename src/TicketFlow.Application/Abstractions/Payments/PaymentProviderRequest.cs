namespace TicketFlow.Application.Abstractions.Payments;

public sealed record PaymentProviderRequest(
    Guid ReservationId,
    Guid UserId,
    decimal Amount,
    string Currency,
    string CardNumber);
