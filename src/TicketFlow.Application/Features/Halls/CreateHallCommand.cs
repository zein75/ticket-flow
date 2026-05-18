using TicketFlow.Domain.Enums;

namespace TicketFlow.Application.Features.Halls;

public sealed record CreateHallCommand(
    Guid UserId,
    UserRole Role,
    string Name,
    int Rows,
    int SeatsPerRow);
