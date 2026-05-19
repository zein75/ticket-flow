namespace TicketFlow.Application.Features.ReservationExpirations;

public sealed record ExpireReservationsResult(IReadOnlyCollection<ExpiredReservationResult> Reservations)
{
    public int Count => Reservations.Count;
}
