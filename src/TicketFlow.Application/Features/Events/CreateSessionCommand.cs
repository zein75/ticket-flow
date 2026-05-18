using TicketFlow.Domain.Enums;

namespace TicketFlow.Application.Features.Events;

public sealed record CreateSessionCommand(
    Guid EventId,
    Guid UserId,
    UserRole Role,
    Guid HallId,
    DateTime StartsAtUtc,
    DateTime EndsAtUtc,
    decimal PriceAmount,
    string PriceCurrency);
