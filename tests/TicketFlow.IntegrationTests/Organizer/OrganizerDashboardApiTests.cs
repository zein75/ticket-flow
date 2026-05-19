using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Cryptography;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using TicketFlow.Domain.Entities;
using TicketFlow.Domain.Enums;
using TicketFlow.Domain.ValueObjects;
using TicketFlow.Infrastructure.Persistence;

namespace TicketFlow.IntegrationTests.Organizer;

public sealed class OrganizerDashboardApiTests
{
    [Fact]
    public async Task Dashboard_ShouldReturnOnlyCurrentOrganizerMetrics()
    {
        await using var app = new OrganizerDashboardApiFactory();
        await app.SeedAsync();
        var client = app.CreateClient();
        await app.AuthorizeAsync(client, "organizer@example.com");

        var response = await client.GetAsync("/api/v1/organizer/dashboard");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var payload = await response.Content.ReadFromJsonAsync<DashboardPayload>();
        Assert.NotNull(payload);
        Assert.Equal(350m, payload.TotalRevenue);
        Assert.Equal("USD", payload.Currency);
        Assert.Equal(3, payload.TicketsSold);
        Assert.Equal(2, payload.SeatsReserved);
        Assert.Equal(0.5m, payload.ConversionRate);
        Assert.Equal(2, payload.SalesBySession.Count);
        Assert.All(payload.SalesBySession, session => Assert.Equal(app.EventId, session.EventId));
    }

    [Fact]
    public async Task EventStats_ShouldReturnEventMetrics_WhenOrganizerOwnsEvent()
    {
        await using var app = new OrganizerDashboardApiFactory();
        await app.SeedAsync();
        var client = app.CreateClient();
        await app.AuthorizeAsync(client, "organizer@example.com");

        var response = await client.GetAsync($"/api/v1/organizer/events/{app.EventId}/stats");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var payload = await response.Content.ReadFromJsonAsync<DashboardPayload>();
        Assert.NotNull(payload);
        Assert.Equal(350m, payload.TotalRevenue);
        Assert.Equal(3, payload.TicketsSold);
        Assert.Equal(2, payload.SeatsReserved);
        Assert.Equal(0.5m, payload.ConversionRate);
    }

    [Fact]
    public async Task EventStats_ShouldReturnForbidden_WhenOrganizerDoesNotOwnEvent()
    {
        await using var app = new OrganizerDashboardApiFactory();
        await app.SeedAsync();
        var client = app.CreateClient();
        await app.AuthorizeAsync(client, "other-organizer@example.com");

        var response = await client.GetAsync($"/api/v1/organizer/events/{app.EventId}/stats");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Dashboard_ShouldReturnAllMetrics_WhenUserIsAdmin()
    {
        await using var app = new OrganizerDashboardApiFactory();
        await app.SeedAsync();
        var client = app.CreateClient();
        await app.AuthorizeAsync(client, "admin@example.com");

        var response = await client.GetAsync("/api/v1/organizer/dashboard");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var payload = await response.Content.ReadFromJsonAsync<DashboardPayload>();
        Assert.NotNull(payload);
        Assert.Equal(1250m, payload.TotalRevenue);
        Assert.Equal(4, payload.TicketsSold);
        Assert.Equal(2, payload.SeatsReserved);
        Assert.Equal(0.6m, payload.ConversionRate);
        Assert.Equal(3, payload.SalesBySession.Count);
    }

    [Fact]
    public async Task Dashboard_ShouldForbidRegularUser()
    {
        await using var app = new OrganizerDashboardApiFactory();
        await app.SeedAsync();
        var client = app.CreateClient();
        await app.AuthorizeAsync(client, "user@example.com");

        var response = await client.GetAsync("/api/v1/organizer/dashboard");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    private sealed record DashboardPayload(
        decimal TotalRevenue,
        string Currency,
        int TicketsSold,
        int SeatsReserved,
        decimal ConversionRate,
        IReadOnlyCollection<SalesBySessionPayload> SalesBySession);

    private sealed record SalesBySessionPayload(
        Guid SessionId,
        Guid EventId,
        DateTime StartsAtUtc,
        decimal Revenue,
        string Currency,
        int TicketsSold);

    private sealed record AuthPayload(
        Guid UserId,
        string Email,
        string Role,
        string AccessToken,
        string RefreshToken,
        DateTime AccessTokenExpiresAtUtc,
        DateTime RefreshTokenExpiresAtUtc);

    private sealed class OrganizerDashboardApiFactory : WebApplicationFactory<Program>
    {
        private readonly string _databaseName = $"ticketflow-organizer-dashboard-tests-{Guid.NewGuid()}";

        public Guid OrganizerId { get; } = Guid.NewGuid();

        public Guid OtherOrganizerId { get; } = Guid.NewGuid();

        public Guid AdminId { get; } = Guid.NewGuid();

        public Guid UserId { get; } = Guid.NewGuid();

        public Guid HallId { get; } = Guid.NewGuid();

        public Guid EventId { get; } = Guid.NewGuid();

        public Guid OtherEventId { get; } = Guid.NewGuid();

        public Guid SessionId { get; } = Guid.NewGuid();

        public Guid SecondSessionId { get; } = Guid.NewGuid();

        public Guid OtherSessionId { get; } = Guid.NewGuid();

        public async Task SeedAsync()
        {
            using var scope = Services.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<TicketFlowDbContext>();
            await dbContext.Database.EnsureDeletedAsync();
            await dbContext.Database.EnsureCreatedAsync();

            dbContext.Users.AddRange(
                new User(OrganizerId, "organizer@example.com", HashPassword("Password123!"), UserRole.Organizer),
                new User(OtherOrganizerId, "other-organizer@example.com", HashPassword("Password123!"), UserRole.Organizer),
                new User(AdminId, "admin@example.com", HashPassword("Password123!"), UserRole.Admin),
                new User(UserId, "user@example.com", HashPassword("Password123!"), UserRole.User));

            var hall = new Hall(HallId, "Main hall", 2, 5);
            var seats = hall.Seats.OrderBy(seat => seat.Row).ThenBy(seat => seat.Number).ToArray();
            dbContext.Halls.Add(hall);

            var ticketEvent = new Event(EventId, "Organizer event", "Owned by organizer", OrganizerId);
            ticketEvent.ChangeStatus(EventStatus.Published);
            var session = new Session(
                SessionId,
                EventId,
                HallId,
                new DateTime(2026, 06, 01, 18, 00, 00, DateTimeKind.Utc),
                new DateTime(2026, 06, 01, 20, 00, 00, DateTimeKind.Utc),
                new Money(100m, "USD"));
            var secondSession = new Session(
                SecondSessionId,
                EventId,
                HallId,
                new DateTime(2026, 06, 02, 18, 00, 00, DateTimeKind.Utc),
                new DateTime(2026, 06, 02, 20, 00, 00, DateTimeKind.Utc),
                new Money(150m, "USD"));
            ticketEvent.AddSession(session);
            ticketEvent.AddSession(secondSession);

            var otherEvent = new Event(OtherEventId, "Other event", "Owned by another organizer", OtherOrganizerId);
            otherEvent.ChangeStatus(EventStatus.Published);
            var otherSession = new Session(
                OtherSessionId,
                OtherEventId,
                HallId,
                new DateTime(2026, 06, 03, 18, 00, 00, DateTimeKind.Utc),
                new DateTime(2026, 06, 03, 20, 00, 00, DateTimeKind.Utc),
                new Money(900m, "USD"));
            otherEvent.AddSession(otherSession);

            dbContext.Events.AddRange(ticketEvent, otherEvent);
            dbContext.Sessions.AddRange(session, secondSession, otherSession);

            var order = CreatePaidOrder(UserId, SessionId, [seats[0].Id, seats[1].Id], new Money(200m, "USD"));
            var secondOrder = CreatePaidOrder(UserId, SecondSessionId, [seats[2].Id], new Money(150m, "USD"));
            var otherOrder = CreatePaidOrder(UserId, OtherSessionId, [seats[3].Id], new Money(900m, "USD"));
            dbContext.Orders.AddRange(order, secondOrder, otherOrder);

            dbContext.Reservations.AddRange(
                new Reservation(Guid.NewGuid(), UserId, SessionId, DateTime.UtcNow.AddMinutes(10), [seats[4].Id, seats[5].Id]),
                CreateConvertedReservation(UserId, SessionId, [seats[0].Id]),
                CreateConvertedReservation(UserId, SecondSessionId, [seats[2].Id]),
                CreateExpiredReservation(UserId, SecondSessionId, [seats[6].Id]),
                CreateConvertedReservation(UserId, OtherSessionId, [seats[3].Id]));

            await dbContext.SaveChangesAsync();
        }

        public async Task AuthorizeAsync(HttpClient client, string email)
        {
            var response = await client.PostAsJsonAsync("/api/v1/auth/login", new
            {
                email,
                password = "Password123!"
            });
            response.EnsureSuccessStatusCode();
            var payload = await response.Content.ReadFromJsonAsync<AuthPayload>();
            Assert.NotNull(payload);
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", payload.AccessToken);
        }

        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.UseEnvironment("Testing");
            builder.UseSetting("ConnectionStrings:Postgres", "Host=unused;Database=ticketflow");
            builder.UseSetting("ConnectionStrings:Redis", "localhost:6379");
            builder.UseSetting("Jwt:Issuer", "TicketFlow.Tests");
            builder.UseSetting("Jwt:Audience", "TicketFlow.Tests");
            builder.UseSetting("Jwt:SigningKey", "ticketflow-tests-signing-key-with-32-plus-chars");
            builder.UseSetting("QrCodes:SigningKey", "ticketflow-tests-qr-signing-key-with-32-plus-chars");

            builder.ConfigureServices(services =>
            {
                services.AddLogging(logging => logging.ClearProviders());
                services.RemoveAll<DbContextOptions<TicketFlowDbContext>>();
                services.RemoveAll<IDbContextOptionsConfiguration<TicketFlowDbContext>>();
                services.AddDbContext<TicketFlowDbContext>(options =>
                {
                    options.UseInMemoryDatabase(_databaseName);
                });
            });
        }

        private static Order CreatePaidOrder(Guid userId, Guid sessionId, IReadOnlyCollection<Guid> seatIds, Money totalAmount)
        {
            var order = new Order(Guid.NewGuid(), userId, sessionId, Guid.NewGuid(), totalAmount);
            order.MarkPaid();

            foreach (var seatId in seatIds)
            {
                var ticketId = Guid.NewGuid();
                order.AddTicket(new Ticket(ticketId, order.Id, sessionId, seatId, $"ticket:{ticketId:N}"));
            }

            return order;
        }

        private static Reservation CreateConvertedReservation(Guid userId, Guid sessionId, IReadOnlyCollection<Guid> seatIds)
        {
            var reservation = new Reservation(Guid.NewGuid(), userId, sessionId, DateTime.UtcNow.AddMinutes(10), seatIds);
            reservation.ConvertToOrder(DateTime.UtcNow);

            return reservation;
        }

        private static Reservation CreateExpiredReservation(Guid userId, Guid sessionId, IReadOnlyCollection<Guid> seatIds)
        {
            var reservation = new Reservation(Guid.NewGuid(), userId, sessionId, DateTime.UtcNow.AddMinutes(-10), seatIds);
            reservation.Expire(DateTime.UtcNow);

            return reservation;
        }

        private static string HashPassword(string password)
        {
            var salt = new byte[16];
            var key = Rfc2898DeriveBytes.Pbkdf2(password, salt, 100_000, HashAlgorithmName.SHA256, 32);

            return string.Join(
                '.',
                "v1",
                100_000,
                Convert.ToBase64String(salt),
                Convert.ToBase64String(key));
        }
    }
}
