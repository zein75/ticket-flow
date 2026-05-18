namespace TicketFlow.Application.Features.Halls;

public interface IHallManagementService
{
    Task<HallResult> CreateAsync(CreateHallCommand command, CancellationToken cancellationToken = default);

    Task<IReadOnlyCollection<HallResult>> ListAsync(CancellationToken cancellationToken = default);

    Task<HallResult> GetAsync(GetHallQuery query, CancellationToken cancellationToken = default);

    Task<IReadOnlyCollection<SeatResult>> ListSessionSeatsAsync(ListSessionSeatsQuery query, CancellationToken cancellationToken = default);
}
