namespace TicketFlow.Api.Contracts.Admin;

public sealed record AdminUserResponse(Guid Id, string Email, string Role, bool IsBlocked);
