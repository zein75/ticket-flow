namespace TicketFlow.Application.Features.Events;

public sealed record EventResult(
    Guid Id,
    string Title,
    string Description,
    Guid OrganizerId,
    string Status);
