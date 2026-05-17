using TicketFlow.Domain.Abstractions;
using TicketFlow.Domain.Enums;
using TicketFlow.Domain.ValueObjects;

namespace TicketFlow.Domain.Entities;

public sealed class Order : Entity
{
    private readonly List<Ticket> _tickets = [];

    private Order()
    {
        TotalAmount = null!;
    }

    public Order(Guid id, Guid userId, Guid sessionId, Guid reservationId, Money totalAmount)
        : base(id)
    {
        if (userId == Guid.Empty)
        {
            throw new ArgumentException("User id cannot be empty.", nameof(userId));
        }

        if (sessionId == Guid.Empty)
        {
            throw new ArgumentException("Session id cannot be empty.", nameof(sessionId));
        }

        if (reservationId == Guid.Empty)
        {
            throw new ArgumentException("Reservation id cannot be empty.", nameof(reservationId));
        }

        UserId = userId;
        SessionId = sessionId;
        ReservationId = reservationId;
        TotalAmount = totalAmount ?? throw new ArgumentNullException(nameof(totalAmount));
        Status = OrderStatus.PendingPayment;
    }

    public Guid UserId { get; }

    public Guid SessionId { get; }

    public Guid ReservationId { get; }

    public Money TotalAmount { get; }

    public OrderStatus Status { get; private set; }

    public IReadOnlyCollection<Ticket> Tickets => _tickets.AsReadOnly();

    public void MarkPaid() => Status = OrderStatus.Paid;

    public void MarkFailed() => Status = OrderStatus.Failed;

    public void Cancel() => Status = OrderStatus.Cancelled;

    public void AddTicket(Ticket ticket)
    {
        ArgumentNullException.ThrowIfNull(ticket);

        if (ticket.OrderId != Id)
        {
            throw new ArgumentException("Ticket belongs to another order.", nameof(ticket));
        }

        _tickets.Add(ticket);
    }
}
