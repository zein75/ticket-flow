using Microsoft.EntityFrameworkCore;
using TicketFlow.Application.Abstractions.Persistence;
using TicketFlow.Domain.Entities;

namespace TicketFlow.Infrastructure.Persistence.Repositories;

internal sealed class TicketRepository : Repository<Ticket>, ITicketRepository
{
    public TicketRepository(TicketFlowDbContext dbContext)
        : base(dbContext)
    {
    }

    public Task<Ticket?> GetByIdWithOrderAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return DbContext.Tickets
            .Include(ticket => ticket.Order)
            .FirstOrDefaultAsync(ticket => ticket.Id == id, cancellationToken);
    }

    public async Task<IReadOnlyCollection<Ticket>> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        return await DbContext.Tickets
            .Include(ticket => ticket.Order)
            .Where(ticket => ticket.Order.UserId == userId)
            .OrderBy(ticket => ticket.OrderId)
            .ThenBy(ticket => ticket.SeatId)
            .ToListAsync(cancellationToken);
    }
}
