namespace TicketFlow.Api.Contracts.Organizer;

public sealed record SalesBySessionResponse(
    Guid SessionId,
    Guid EventId,
    DateTime StartsAtUtc,
    decimal Revenue,
    string Currency,
    int TicketsSold);
