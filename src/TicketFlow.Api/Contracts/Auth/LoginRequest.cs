namespace TicketFlow.Api.Contracts.Auth;

public sealed record LoginRequest(
    [property: System.ComponentModel.DataAnnotations.Required]
    [property: System.ComponentModel.DataAnnotations.EmailAddress]
    [property: System.ComponentModel.DataAnnotations.MaxLength(320)]
    string Email,
    [property: System.ComponentModel.DataAnnotations.Required]
    string Password);
