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
}
