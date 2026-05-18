using Microsoft.EntityFrameworkCore;
using TicketFlow.Application.Abstractions.Persistence;
using TicketFlow.Domain.Entities;

namespace TicketFlow.Infrastructure.Persistence.Repositories;

internal sealed class SessionRepository : Repository<Session>, ISessionRepository
{
    public SessionRepository(TicketFlowDbContext dbContext)
        : base(dbContext)
    {
    }

    public async Task<IReadOnlyCollection<Session>> GetByEventIdAsync(Guid eventId, CancellationToken cancellationToken = default)
    {
        return await DbContext.Sessions
            .Where(session => session.EventId == eventId)
            .OrderBy(session => session.StartsAtUtc)
            .ToListAsync(cancellationToken);
    }
}
