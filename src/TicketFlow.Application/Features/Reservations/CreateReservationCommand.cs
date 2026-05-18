namespace TicketFlow.Application.Features.Reservations;

public sealed record CreateReservationCommand(Guid UserId, Guid SessionId, IReadOnlyCollection<Guid> SeatIds);
