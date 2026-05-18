using System.ComponentModel.DataAnnotations;
using TicketFlow.Api.Validation;

namespace TicketFlow.Api.Contracts.Events;

public sealed record CreateSessionRequest(
    [Required]
    Guid HallId,

    [Required]
    DateTime StartsAtUtc,

    [Required]
    DateTime EndsAtUtc,

    [Range(0, 999999999)]
    decimal PriceAmount,

    [Required]
    [StringLength(3, MinimumLength = 3)]
    [NotBlank]
    string PriceCurrency);
