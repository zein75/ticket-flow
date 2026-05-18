using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using TicketFlow.Application.Abstractions.Auth;
using TicketFlow.Application.Abstractions.Common;
using TicketFlow.Application.Abstractions.Persistence;
using TicketFlow.Infrastructure.Auth;
using TicketFlow.Infrastructure.Common;
using TicketFlow.Infrastructure.Persistence;
using TicketFlow.Infrastructure.Persistence.Repositories;

namespace TicketFlow.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("Postgres")
            ?? throw new InvalidOperationException("Connection string 'Postgres' is not configured.");

        services.AddDbContext<TicketFlowDbContext>(options =>
        {
            options.UseNpgsql(connectionString);
        });

        services.AddOptions<JwtOptions>()
            .Bind(configuration.GetSection(JwtOptions.SectionName))
            .ValidateOnStart();
        services.AddSingleton<Microsoft.Extensions.Options.IValidateOptions<JwtOptions>, JwtOptionsValidator>();
        services.AddSingleton<IClock, SystemClock>();
        services.AddSingleton<IPasswordHasher, Pbkdf2PasswordHasher>();
        services.AddSingleton<IRefreshTokenGenerator, RefreshTokenGenerator>();
        services.AddSingleton<ITokenService, JwtTokenService>();

        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<IRefreshTokenRepository, RefreshTokenRepository>();
        services.AddScoped<IEventRepository, EventRepository>();
        services.AddScoped<ISessionRepository, SessionRepository>();
        services.AddScoped<IHallRepository, HallRepository>();
        services.AddScoped<IReservationRepository, ReservationRepository>();
        services.AddScoped<IOrderRepository, OrderRepository>();
        services.AddScoped<IUnitOfWork, UnitOfWork>();

        return services;
    }
}
