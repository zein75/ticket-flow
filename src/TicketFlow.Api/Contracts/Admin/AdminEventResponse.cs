namespace TicketFlow.Api.Contracts.Admin;

public sealed record AdminEventResponse(
    Guid Id,
    string Title,
    string Description,
    Guid OrganizerId,
    string Status);
