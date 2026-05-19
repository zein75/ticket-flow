using System.ComponentModel.DataAnnotations;
using TicketFlow.Api.Validation;

namespace TicketFlow.Api.Contracts.Payments;

public sealed record PayReservationRequest(
    [Required]
    [NotBlank]
    [MaxLength(32)]
    string CardNumber);
