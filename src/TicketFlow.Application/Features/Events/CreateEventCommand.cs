using TicketFlow.Domain.Enums;

namespace TicketFlow.Application.Features.Events;

public sealed record CreateEventCommand(
    Guid UserId,
    UserRole Role,
    string Title,
    string Description,
    EventStatus Status);
