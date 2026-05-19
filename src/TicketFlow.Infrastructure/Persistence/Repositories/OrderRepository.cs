using Microsoft.EntityFrameworkCore;
using TicketFlow.Application.Abstractions.Persistence;
using TicketFlow.Domain.Entities;
using TicketFlow.Domain.Enums;

namespace TicketFlow.Infrastructure.Persistence.Repositories;

internal sealed class OrderRepository : Repository<Order>, IOrderRepository
{
    public OrderRepository(TicketFlowDbContext dbContext)
        : base(dbContext)
    {
    }

    public async Task<IReadOnlyCollection<Order>> GetAllWithTicketsAsync(CancellationToken cancellationToken = default)
    {
        return await DbContext.Orders
            .Include(order => order.Tickets)
            .OrderBy(order => order.Id)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyCollection<Order>> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        return await DbContext.Orders
            .Where(order => order.UserId == userId)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyCollection<Order>> GetPaidBySessionIdsWithTicketsAsync(
        IReadOnlyCollection<Guid> sessionIds,
        CancellationToken cancellationToken = default)
    {
        return await DbContext.Orders
            .Include(order => order.Tickets)
            .Where(order => sessionIds.Contains(order.SessionId) && order.Status == OrderStatus.Paid)
            .ToListAsync(cancellationToken);
    }
}
