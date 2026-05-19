namespace TicketFlow.Application.Features.Tickets;

public sealed record ValidateTicketCommand(string Payload, Guid UserId, string Role);
