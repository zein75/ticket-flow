namespace TicketFlow.Application.Features.Tickets;

public sealed record GetTicketQrCodeQuery(Guid TicketId, Guid UserId, string Role);
