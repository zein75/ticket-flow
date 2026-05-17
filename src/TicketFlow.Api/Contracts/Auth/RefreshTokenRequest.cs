namespace TicketFlow.Api.Contracts.Auth;

public sealed record RefreshTokenRequest(
    [property: System.ComponentModel.DataAnnotations.Required]
    string RefreshToken);
