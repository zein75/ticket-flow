namespace TicketFlow.Application.Features.Admin;

public sealed record ModeratedEventResult(
    Guid Id,
    string Title,
    string Description,
    Guid OrganizerId,
    string Status);
