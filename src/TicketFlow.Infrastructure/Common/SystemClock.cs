using TicketFlow.Application.Abstractions.Common;

namespace TicketFlow.Infrastructure.Common;

internal sealed class SystemClock : IClock
{
    public DateTime UtcNow => DateTime.UtcNow;
}
