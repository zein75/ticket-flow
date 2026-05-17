namespace TicketFlow.Application.Features.Auth;

public sealed record LoginCommand(string Email, string Password);
