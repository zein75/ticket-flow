namespace TicketFlow.Application.Features.Admin;

public sealed record BlockUserCommand(Guid UserId, Guid AdminUserId);
