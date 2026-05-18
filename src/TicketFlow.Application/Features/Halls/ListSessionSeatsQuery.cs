using TicketFlow.Domain.Enums;

namespace TicketFlow.Application.Features.Halls;

public sealed record ListSessionSeatsQuery(Guid SessionId, Guid? UserId, UserRole? Role);
