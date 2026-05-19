namespace TicketFlow.Application.Features.Admin;

public sealed class AdminException : Exception
{
    private AdminException(string code, string message)
        : base(message)
    {
        Code = code;
    }

    public string Code { get; }

    public static AdminException UserNotFound() =>
        new("user_not_found", "User was not found.");

    public static AdminException CannotBlockSelf() =>
        new("cannot_block_self", "Administrator cannot block their own account.");

    public static AdminException EventNotFound() =>
        new("event_not_found", "Event was not found.");
}
