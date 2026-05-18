namespace TicketFlow.Api.Contracts.Events;

public sealed record SessionResponse(
    Guid Id,
    Guid EventId,
    Guid HallId,
    DateTime StartsAtUtc,
    DateTime EndsAtUtc,
    decimal PriceAmount,
    string PriceCurrency);
