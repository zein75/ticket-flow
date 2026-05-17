using System.ComponentModel.DataAnnotations;

namespace TicketFlow.Api.Contracts.Auth;

public sealed record RegisterRequest(
    [property: Required]
    [property: EmailAddress]
    [property: MaxLength(320)]
    string Email,
    [property: Required]
    [property: MinLength(8)]
    [property: MaxLength(128)]
    string Password);
