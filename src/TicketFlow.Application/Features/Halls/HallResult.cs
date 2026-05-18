namespace TicketFlow.Application.Features.Halls;

public sealed record HallResult(
    Guid Id,
    string Name,
    int Rows,
    int SeatsPerRow,
    IReadOnlyCollection<SeatResult> Seats);
