using TicketFlow.Domain.Enums;

namespace TicketFlow.Application.Features.Events;

public sealed record ListEventsQuery(Guid? UserId, UserRole? Role);
