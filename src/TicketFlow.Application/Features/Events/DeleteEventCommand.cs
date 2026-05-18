using TicketFlow.Domain.Enums;

namespace TicketFlow.Application.Features.Events;

public sealed record DeleteEventCommand(Guid EventId, Guid UserId, UserRole Role);
