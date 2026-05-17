namespace TicketFlow.Api.Contracts.Auth;

public sealed record LogoutRequest(
    [property: System.ComponentModel.DataAnnotations.Required]
    string RefreshToken);
