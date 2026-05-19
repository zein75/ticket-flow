using TicketFlow.Domain.Enums;

namespace TicketFlow.Application.Features.OrganizerDashboard;

public sealed record GetOrganizerDashboardQuery(Guid UserId, UserRole Role);
