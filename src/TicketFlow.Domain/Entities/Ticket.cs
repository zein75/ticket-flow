using TicketFlow.Domain.Abstractions;
using TicketFlow.Domain.Enums;

namespace TicketFlow.Domain.Entities;

public sealed class Ticket : Entity
{
    private Ticket()
    {
        QrCodePayload = string.Empty;
    }

    public Ticket(Guid id, Guid orderId, Guid sessionId, Guid seatId, string qrCodePayload)
        : base(id)
    {
        if (orderId == Guid.Empty)
        {
            throw new ArgumentException("Order id cannot be empty.", nameof(orderId));
        }

        if (sessionId == Guid.Empty)
        {
            throw new ArgumentException("Session id cannot be empty.", nameof(sessionId));
        }

        if (seatId == Guid.Empty)
        {
            throw new ArgumentException("Seat id cannot be empty.", nameof(seatId));
        }

        if (string.IsNullOrWhiteSpace(qrCodePayload))
        {
            throw new ArgumentException("QR code payload is required.", nameof(qrCodePayload));
        }

        OrderId = orderId;
        SessionId = sessionId;
        SeatId = seatId;
        QrCodePayload = qrCodePayload;
        Status = TicketStatus.Active;
    }

    public Guid OrderId { get; }

    public Guid SessionId { get; }

    public Guid SeatId { get; }

    public string QrCodePayload { get; private set; }

    public TicketStatus Status { get; private set; }

    public void MarkUsed() => Status = TicketStatus.Used;

    public void Cancel() => Status = TicketStatus.Cancelled;
}
