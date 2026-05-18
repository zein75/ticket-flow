using TicketFlow.Domain.Enums;

namespace TicketFlow.Application.Features.Events;

public sealed record UpdateEventCommand(
    Guid EventId,
    Guid UserId,
    UserRole Role,
    string Title,
    string Description,
    EventStatus Status);
