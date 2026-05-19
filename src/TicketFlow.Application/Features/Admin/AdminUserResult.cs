namespace TicketFlow.Application.Features.Admin;

public sealed record AdminUserResult(Guid Id, string Email, string Role, bool IsBlocked);
