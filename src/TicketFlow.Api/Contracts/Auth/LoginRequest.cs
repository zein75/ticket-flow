namespace TicketFlow.Api.Contracts.Auth;

public sealed record LoginRequest(
    [System.ComponentModel.DataAnnotations.Required]
    [System.ComponentModel.DataAnnotations.EmailAddress]
    [System.ComponentModel.DataAnnotations.MaxLength(320)]
    [TicketFlow.Api.Validation.NotBlank]
    string Email,
    [System.ComponentModel.DataAnnotations.Required]
    [TicketFlow.Api.Validation.NotBlank]
    string Password);
