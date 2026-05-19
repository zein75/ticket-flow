namespace TicketFlow.Application.Features.Payments;

public sealed record PayReservationCommand(Guid ReservationId, Guid UserId, string CardNumber);
