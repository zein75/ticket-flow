namespace TicketFlow.Application.Features.SeatMaps;

public sealed class SeatMapException : Exception
{
    private SeatMapException(string code, string message)
        : base(message)
    {
        Code = code;
    }

    public string Code { get; }

    public static SeatMapException SessionNotFound() =>
        new("session_not_found", "Session was not found.");
}
