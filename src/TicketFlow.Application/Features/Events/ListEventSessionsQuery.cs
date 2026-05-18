using TicketFlow.Domain.Enums;

namespace TicketFlow.Application.Features.Events;

public sealed record ListEventSessionsQuery(Guid EventId, Guid? UserId, UserRole? Role);
