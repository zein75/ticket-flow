namespace TicketFlow.Api.Contracts.Halls;

public sealed record HallResponse(
    Guid Id,
    string Name,
    int Rows,
    int SeatsPerRow,
    IReadOnlyCollection<SeatResponse> Seats);
