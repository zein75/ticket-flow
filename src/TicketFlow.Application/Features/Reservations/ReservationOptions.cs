namespace TicketFlow.Application.Features.Reservations;

public sealed class ReservationOptions
{
    public const string SectionName = "Reservations";

    public int TtlMinutes { get; init; } = 10;
}
