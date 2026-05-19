namespace TicketFlow.Worker;

public sealed class ExpiredReservationWorkerOptions
{
    public const string SectionName = "ExpiredReservations";

    public int IntervalSeconds { get; init; } = 30;
}
