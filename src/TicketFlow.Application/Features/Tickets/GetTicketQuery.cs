namespace TicketFlow.Application.Features.Tickets;

public sealed record GetTicketQuery(Guid TicketId, Guid UserId, string Role);
