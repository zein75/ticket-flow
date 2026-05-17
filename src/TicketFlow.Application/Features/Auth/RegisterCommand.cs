namespace TicketFlow.Application.Features.Auth;

public sealed record RegisterCommand(
    string Email,
    string Password);
