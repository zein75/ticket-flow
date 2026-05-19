namespace TicketFlow.Api.Contracts.Organizer;

public sealed record OrganizerDashboardResponse(
    decimal TotalRevenue,
    string Currency,
    int TicketsSold,
    int SeatsReserved,
    decimal ConversionRate,
    IReadOnlyCollection<SalesBySessionResponse> SalesBySession);
