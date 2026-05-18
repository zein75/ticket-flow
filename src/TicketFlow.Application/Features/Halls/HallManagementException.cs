namespace TicketFlow.Application.Features.Halls;

public sealed class HallManagementException : Exception
{
    private HallManagementException(string code, string message)
        : base(message)
    {
        Code = code;
    }

    public string Code { get; }

    public static HallManagementException NotFound() =>
        new("hall_not_found", "Hall was not found.");

    public static HallManagementException SessionNotFound() =>
        new("session_not_found", "Session was not found.");

    public static HallManagementException RoleNotAllowed() =>
        new("role_not_allowed", "Only organizers and admins can manage halls.");

    public static HallManagementException Validation(string message) =>
        new("hall_validation_failed", message);
}
