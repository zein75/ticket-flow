namespace TicketFlow.Application.Abstractions.Common;

public interface IClock
{
    DateTime UtcNow { get; }
}
