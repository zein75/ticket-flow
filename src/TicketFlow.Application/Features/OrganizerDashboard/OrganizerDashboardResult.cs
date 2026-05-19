namespace TicketFlow.Application.Features.OrganizerDashboard;

public sealed record OrganizerDashboardResult(
    decimal TotalRevenue,
    string Currency,
    int TicketsSold,
    int SeatsReserved,
    decimal ConversionRate,
    IReadOnlyCollection<SalesBySessionResult> SalesBySession);
