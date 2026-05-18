using System.ComponentModel.DataAnnotations;
using TicketFlow.Api.Validation;

namespace TicketFlow.Api.Contracts.Halls;

public sealed record CreateHallRequest(
    [Required]
    [MaxLength(200)]
    [NotBlank]
    string Name,

    [Range(1, 200)]
    int Rows,

    [Range(1, 500)]
    int SeatsPerRow);
