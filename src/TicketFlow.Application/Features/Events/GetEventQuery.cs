using TicketFlow.Domain.Enums;

namespace TicketFlow.Application.Features.Events;

public sealed record GetEventQuery(Guid EventId, Guid? UserId, UserRole? Role);
