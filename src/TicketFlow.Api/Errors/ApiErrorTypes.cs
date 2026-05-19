namespace TicketFlow.Api.Errors;

public static class ApiErrorTypes
{
    public const string Base = "https://ticketflow/errors/";

    public const string Validation = Base + "validation";

    public const string InternalServerError = Base + "internal-server-error";
}
