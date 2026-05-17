using System.ComponentModel.DataAnnotations;
using TicketFlow.Api.Validation;

namespace TicketFlow.Api.Contracts.Auth;

public sealed record RegisterRequest(
    [Required]
    [EmailAddress]
    [MaxLength(320)]
    [NotBlank]
    string Email,
    [Required]
    [MinLength(8)]
    [MaxLength(128)]
    [NotBlank]
    string Password);
