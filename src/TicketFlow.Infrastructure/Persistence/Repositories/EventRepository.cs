using Microsoft.EntityFrameworkCore;
using TicketFlow.Application.Abstractions.Persistence;
using TicketFlow.Domain.Entities;
using TicketFlow.Domain.Enums;

namespace TicketFlow.Infrastructure.Persistence.Repositories;

internal sealed class EventRepository : Repository<Event>, IEventRepository
{
    public EventRepository(TicketFlowDbContext dbContext)
        : base(dbContext)
    {
    }

    public async Task<IReadOnlyCollection<Event>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return await DbContext.Events
            .OrderBy(ticketEvent => ticketEvent.Title)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyCollection<Event>> GetByOrganizerIdAsync(Guid organizerId, CancellationToken cancellationToken = default)
    {
        return await DbContext.Events
            .Where(ticketEvent => ticketEvent.OrganizerId == organizerId)
            .OrderBy(ticketEvent => ticketEvent.Title)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyCollection<Event>> GetPublishedAsync(CancellationToken cancellationToken = default)
    {
        return await DbContext.Events
            .Where(ticketEvent => ticketEvent.Status == EventStatus.Published)
            .OrderBy(ticketEvent => ticketEvent.Title)
            .ToListAsync(cancellationToken);
    }
}
