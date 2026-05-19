using TicketFlow.Domain.Enums;

namespace TicketFlow.Application.Features.OrganizerDashboard;

public sealed record GetOrganizerEventStatsQuery(Guid EventId, Guid UserId, UserRole Role);
