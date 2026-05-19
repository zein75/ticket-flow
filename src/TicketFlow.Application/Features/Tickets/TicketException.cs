namespace TicketFlow.Application.Features.Tickets;

public sealed class TicketException : Exception
{
    private TicketException(string code, string message)
        : base(message)
    {
        Code = code;
    }

    public string Code { get; }

    public static TicketException NotFound() =>
        new("ticket_not_found", "Ticket was not found.");

    public static TicketException AccessDenied() =>
        new("ticket_access_denied", "You do not have access to this ticket.");

    public static TicketException InvalidPayload() =>
        new("invalid_ticket_payload", "Ticket QR payload is invalid.");
}
