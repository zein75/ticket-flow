namespace TicketFlow.Application.Features.Events;

public sealed class EventManagementException : Exception
{
    private EventManagementException(string code, string message)
        : base(message)
    {
        Code = code;
    }

    public string Code { get; }

    public static EventManagementException NotFound() =>
        new("event_not_found", "Event was not found.");

    public static EventManagementException Forbidden() =>
        new("event_access_denied", "You do not have permission to manage this event.");

    public static EventManagementException RoleNotAllowed() =>
        new("role_not_allowed", "Only organizers and admins can manage events.");

    public static EventManagementException HallNotFound() =>
        new("hall_not_found", "Hall was not found.");

    public static EventManagementException Validation(string message) =>
        new("event_validation_failed", message);
}
