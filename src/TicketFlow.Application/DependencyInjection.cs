using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using TicketFlow.Application.Features.Admin;
using TicketFlow.Application.Features.Auth;
using TicketFlow.Application.Features.Events;
using TicketFlow.Application.Features.Halls;
using TicketFlow.Application.Features.OrganizerDashboard;
using TicketFlow.Application.Features.Payments;
using TicketFlow.Application.Features.ReservationExpirations;
using TicketFlow.Application.Features.Reservations;
using TicketFlow.Application.Features.SeatMaps;
using TicketFlow.Application.Features.Tickets;

namespace TicketFlow.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<AuthOptions>(configuration.GetSection(AuthOptions.SectionName));
        services.Configure<ReservationOptions>(configuration.GetSection(ReservationOptions.SectionName));
        services.AddScoped<IAdminService, AdminService>();
        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<IEventManagementService, EventManagementService>();
        services.AddScoped<IHallManagementService, HallManagementService>();
        services.AddScoped<IOrganizerDashboardService, OrganizerDashboardService>();
        services.AddScoped<IPaymentService, PaymentService>();
        services.AddScoped<IExpiredReservationService, ExpiredReservationService>();
        services.AddScoped<IReservationService, ReservationService>();
        services.AddScoped<ISeatMapService, SeatMapService>();
        services.AddScoped<ITicketService, TicketService>();

        return services;
    }
}
