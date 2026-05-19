namespace TicketFlow.Application.Features.Tickets;

public sealed record TicketValidationResult(Guid TicketId, Guid OrderId, string Status, bool IsValid);
