using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using StackExchange.Redis;
using TicketFlow.Application.Abstractions.Auth;
using TicketFlow.Application.Abstractions.Common;
using TicketFlow.Application.Abstractions.Payments;
using TicketFlow.Application.Abstractions.Persistence;
using TicketFlow.Application.Abstractions.QrCodes;
using TicketFlow.Application.Abstractions.Reservations;
using TicketFlow.Infrastructure.Auth;
using TicketFlow.Infrastructure.Common;
using TicketFlow.Infrastructure.Persistence;
using TicketFlow.Infrastructure.Persistence.Repositories;
using TicketFlow.Infrastructure.Payments;
using TicketFlow.Infrastructure.QrCodes;
using TicketFlow.Infrastructure.Redis;

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

        var redisConnectionString = configuration.GetConnectionString("Redis")
            ?? throw new InvalidOperationException("Connection string 'Redis' is not configured.");

        services.AddOptions<JwtOptions>()
            .Bind(configuration.GetSection(JwtOptions.SectionName))
            .ValidateOnStart();
        services.AddOptions<QrCodeOptions>()
            .Bind(configuration.GetSection(QrCodeOptions.SectionName))
            .Validate(options => !string.IsNullOrWhiteSpace(options.SigningKey) && options.SigningKey.Length >= 32, "QR code signing key must contain at least 32 characters.")
            .ValidateOnStart();
        services.AddSingleton<Microsoft.Extensions.Options.IValidateOptions<JwtOptions>, JwtOptionsValidator>();
        services.AddSingleton<IConnectionMultiplexer>(_ => ConnectionMultiplexer.Connect(redisConnectionString));
        services.AddSingleton<IClock, SystemClock>();
        services.AddSingleton<IPasswordHasher, Pbkdf2PasswordHasher>();
        services.AddSingleton<IRefreshTokenGenerator, RefreshTokenGenerator>();
        services.AddSingleton<ITokenService, JwtTokenService>();
        services.AddSingleton<ISeatLockStore, RedisSeatLockStore>();
        services.AddSingleton<IPaymentProvider, MockPaymentProvider>();
        services.AddSingleton<IQrCodePayloadProtector, HmacQrCodePayloadProtector>();
        services.AddSingleton<IQrCodeImageGenerator, QrCodeImageGenerator>();

        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<IRefreshTokenRepository, RefreshTokenRepository>();
        services.AddScoped<IEventRepository, EventRepository>();
        services.AddScoped<ISessionRepository, SessionRepository>();
        services.AddScoped<IHallRepository, HallRepository>();
        services.AddScoped<ISeatStatusRepository, SeatStatusRepository>();
        services.AddScoped<IReservationRepository, ReservationRepository>();
        services.AddScoped<IOrderRepository, OrderRepository>();
        services.AddScoped<IPaymentRepository, PaymentRepository>();
        services.AddScoped<ITicketRepository, TicketRepository>();
        services.AddScoped<IUnitOfWork, UnitOfWork>();

        return services;
    }
}
