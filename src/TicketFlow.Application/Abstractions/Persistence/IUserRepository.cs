using TicketFlow.Domain.Entities;

namespace TicketFlow.Application.Abstractions.Persistence;

public interface IUserRepository : IRepository<User>
{
    Task<IReadOnlyCollection<User>> GetAllAsync(CancellationToken cancellationToken = default);

    Task<User?> GetByEmailAsync(string email, CancellationToken cancellationToken = default);
}
