namespace TicketFlow.Domain.Exceptions;

public sealed class InvalidReservationStateException : DomainException
{
    public InvalidReservationStateException(string message)
        : base(message)
    {
    }
}
