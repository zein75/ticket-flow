namespace TicketFlow.Application.Features.ReservationExpirations;

public interface IExpiredReservationService
{
    Task<ExpireReservationsResult> ExpireAsync(CancellationToken cancellationToken = default);
}
