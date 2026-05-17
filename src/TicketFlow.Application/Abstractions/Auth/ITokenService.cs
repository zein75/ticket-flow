using TicketFlow.Domain.Entities;

namespace TicketFlow.Application.Abstractions.Auth;

public interface ITokenService
{
    string CreateAccessToken(User user);
}
