namespace TicketFlow.Api.Contracts.Auth;

public sealed record RefreshTokenRequest(
    [System.ComponentModel.DataAnnotations.Required]
    [TicketFlow.Api.Validation.NotBlank]
    string RefreshToken);
