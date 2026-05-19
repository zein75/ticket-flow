namespace TicketFlow.Application.Features.Tickets;

public sealed record TicketQrCodeResult(Guid TicketId, byte[] Image, string ContentType);
