namespace TicketFlow.Application.Features.Reservations;

public interface IReservationService
{
    Task<ReservationResult> CreateAsync(CreateReservationCommand command, CancellationToken cancellationToken = default);
}
