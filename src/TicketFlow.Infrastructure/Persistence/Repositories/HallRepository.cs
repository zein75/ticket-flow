using Microsoft.EntityFrameworkCore;
using TicketFlow.Application.Abstractions.Persistence;
using TicketFlow.Domain.Entities;

namespace TicketFlow.Infrastructure.Persistence.Repositories;

internal sealed class HallRepository : Repository<Hall>, IHallRepository
{
    public HallRepository(TicketFlowDbContext dbContext)
        : base(dbContext)
    {
    }

    public Task<bool> ExistsAsync(Guid hallId, CancellationToken cancellationToken = default)
    {
        return DbContext.Halls.AnyAsync(hall => hall.Id == hallId, cancellationToken);
    }

    public async Task<IReadOnlyCollection<Hall>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return await DbContext.Halls
            .Include(hall => hall.Seats)
            .OrderBy(hall => hall.Name)
            .ToListAsync(cancellationToken);
    }

    public Task<Hall?> GetByIdWithSeatsAsync(Guid hallId, CancellationToken cancellationToken = default)
    {
        return DbContext.Halls
            .Include(hall => hall.Seats)
            .FirstOrDefaultAsync(hall => hall.Id == hallId, cancellationToken);
    }
}
