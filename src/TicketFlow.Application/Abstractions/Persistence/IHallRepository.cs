using TicketFlow.Domain.Entities;

namespace TicketFlow.Application.Abstractions.Persistence;

public interface IHallRepository : IRepository<Hall>
{
    Task<bool> ExistsAsync(Guid hallId, CancellationToken cancellationToken = default);

    Task<IReadOnlyCollection<Hall>> GetAllAsync(CancellationToken cancellationToken = default);

    Task<Hall?> GetByIdWithSeatsAsync(Guid hallId, CancellationToken cancellationToken = default);
}
