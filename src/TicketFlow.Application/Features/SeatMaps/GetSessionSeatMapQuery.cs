using TicketFlow.Domain.Enums;

namespace TicketFlow.Application.Features.SeatMaps;

public sealed record GetSessionSeatMapQuery(Guid SessionId, Guid? UserId, UserRole? Role);
