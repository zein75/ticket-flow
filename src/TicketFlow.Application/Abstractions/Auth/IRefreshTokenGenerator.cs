namespace TicketFlow.Application.Abstractions.Auth;

public interface IRefreshTokenGenerator
{
    string Generate();

    string Hash(string refreshToken);
}
