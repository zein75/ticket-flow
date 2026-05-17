namespace TicketFlow.Api.Auth;

public static class AuthorizationPolicies
{
    public const string AdminOnly = "AdminOnly";

    public const string OrganizerOnly = "OrganizerOnly";

    public const string OrganizerOrAdmin = "OrganizerOrAdmin";
}
