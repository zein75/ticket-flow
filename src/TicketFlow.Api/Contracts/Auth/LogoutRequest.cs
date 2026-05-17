namespace TicketFlow.Api.Contracts.Auth;

public sealed record LogoutRequest(
    [System.ComponentModel.DataAnnotations.Required]
    [TicketFlow.Api.Validation.NotBlank]
    string RefreshToken);
