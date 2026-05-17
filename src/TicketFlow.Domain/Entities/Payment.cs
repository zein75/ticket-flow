using TicketFlow.Domain.Abstractions;
using TicketFlow.Domain.Enums;
using TicketFlow.Domain.ValueObjects;

namespace TicketFlow.Domain.Entities;

public sealed class Payment : Entity
{
    private Payment()
    {
        Amount = null!;
    }

    public Payment(Guid id, Guid orderId, Money amount)
        : base(id)
    {
        if (orderId == Guid.Empty)
        {
            throw new ArgumentException("Order id cannot be empty.", nameof(orderId));
        }

        OrderId = orderId;
        Amount = amount ?? throw new ArgumentNullException(nameof(amount));
        Status = PaymentStatus.Pending;
    }

    public Guid OrderId { get; }

    public Money Amount { get; }

    public PaymentStatus Status { get; private set; }

    public string? ProviderPaymentId { get; private set; }

    public string? FailureReason { get; private set; }

    public void MarkSucceeded(string providerPaymentId)
    {
        ProviderPaymentId = string.IsNullOrWhiteSpace(providerPaymentId)
            ? throw new ArgumentException("Provider payment id is required.", nameof(providerPaymentId))
            : providerPaymentId;
        Status = PaymentStatus.Succeeded;
        FailureReason = null;
    }

    public void MarkFailed(string failureReason)
    {
        FailureReason = string.IsNullOrWhiteSpace(failureReason)
            ? throw new ArgumentException("Failure reason is required.", nameof(failureReason))
            : failureReason;
        Status = PaymentStatus.Failed;
    }
}
