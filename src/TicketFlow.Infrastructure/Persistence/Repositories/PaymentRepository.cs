using TicketFlow.Application.Abstractions.Persistence;
using TicketFlow.Domain.Entities;

namespace TicketFlow.Infrastructure.Persistence.Repositories;

internal sealed class PaymentRepository : Repository<Payment>, IPaymentRepository
{
    public PaymentRepository(TicketFlowDbContext dbContext)
        : base(dbContext)
    {
    }
}
