namespace TicketFlow.Application.Features.OrganizerDashboard;

public sealed record SalesBySessionResult(
    Guid SessionId,
    Guid EventId,
    DateTime StartsAtUtc,
    decimal Revenue,
    string Currency,
    int TicketsSold);
