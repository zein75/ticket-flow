namespace TicketFlow.Application.Features.OrganizerDashboard;

public sealed class OrganizerDashboardException : Exception
{
    private OrganizerDashboardException(string code, string message)
        : base(message)
    {
        Code = code;
    }

    public string Code { get; }

    public static OrganizerDashboardException EventNotFound() =>
        new("event_not_found", "Event was not found.");

    public static OrganizerDashboardException AccessDenied() =>
        new("event_access_denied", "You do not have access to this event.");
}
