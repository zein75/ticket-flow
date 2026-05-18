using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using TicketFlow.Application.Features.Auth;
using TicketFlow.Application.Features.Events;
using TicketFlow.Application.Features.Halls;

namespace TicketFlow.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<AuthOptions>(configuration.GetSection(AuthOptions.SectionName));
        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<IEventManagementService, EventManagementService>();
        services.AddScoped<IHallManagementService, HallManagementService>();

        return services;
    }
}
