namespace TicketFlow.Application.Features.OrganizerDashboard;

public interface IOrganizerDashboardService
{
    Task<OrganizerDashboardResult> GetDashboardAsync(GetOrganizerDashboardQuery query, CancellationToken cancellationToken = default);

    Task<OrganizerDashboardResult> GetEventStatsAsync(GetOrganizerEventStatsQuery query, CancellationToken cancellationToken = default);
}
