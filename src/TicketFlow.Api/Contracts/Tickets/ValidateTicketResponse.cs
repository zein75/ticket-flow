namespace TicketFlow.Api.Contracts.Tickets;

public sealed record ValidateTicketResponse(Guid TicketId, Guid OrderId, string Status, bool IsValid);
