using System.ComponentModel.DataAnnotations;

namespace TicketFlow.Api.Contracts.Reservations;

public sealed record CreateReservationRequest(
    [Required]
    Guid SessionId,

    [Required]
    [MinLength(1)]
    IReadOnlyCollection<Guid> SeatIds);
