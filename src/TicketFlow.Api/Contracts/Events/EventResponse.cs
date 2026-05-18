namespace TicketFlow.Api.Contracts.Events;

public sealed record EventResponse(
    Guid Id,
    string Title,
    string Description,
    Guid OrganizerId,
    string Status);
