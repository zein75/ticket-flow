namespace TicketFlow.Application.Features.Events;

public sealed record SessionResult(
    Guid Id,
    Guid EventId,
    Guid HallId,
    DateTime StartsAtUtc,
    DateTime EndsAtUtc,
    decimal PriceAmount,
    string PriceCurrency);
