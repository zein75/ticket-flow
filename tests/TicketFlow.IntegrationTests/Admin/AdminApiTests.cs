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

namespace TicketFlow.IntegrationTests.Admin;

public sealed class AdminApiTests
{
    [Fact]
    public async Task ListUsers_ShouldReturnAllUsers_WhenAdmin()
    {
        await using var app = new AdminApiFactory();
        await app.SeedAsync();
        var client = app.CreateClient();
        await app.AuthorizeAsync(client, "admin@example.com");

        var response = await client.GetAsync("/api/v1/admin/users");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var payload = await response.Content.ReadFromJsonAsync<IReadOnlyCollection<AdminUserPayload>>();
        Assert.NotNull(payload);
        Assert.Equal(3, payload.Count);
        Assert.Contains(payload, user => user.Email == "organizer@example.com");
    }

    [Fact]
    public async Task ListUsers_ShouldForbidRegularUser()
    {
        await using var app = new AdminApiFactory();
        await app.SeedAsync();
        var client = app.CreateClient();
        await app.AuthorizeAsync(client, "user@example.com");

        var response = await client.GetAsync("/api/v1/admin/users");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task BlockUser_ShouldBlockUser_WhenAdmin()
    {
        await using var app = new AdminApiFactory();
        await app.SeedAsync();
        var client = app.CreateClient();
        await app.AuthorizeAsync(client, "admin@example.com");

        var response = await client.PatchAsync($"/api/v1/admin/users/{app.UserId}/block", null);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var payload = await response.Content.ReadFromJsonAsync<AdminUserPayload>();
        Assert.NotNull(payload);
        Assert.Equal(app.UserId, payload.Id);
        Assert.True(payload.IsBlocked);

        using var scope = app.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<TicketFlowDbContext>();
        var user = await dbContext.Users.SingleAsync(user => user.Id == app.UserId);
        Assert.True(user.IsBlocked);
    }

    [Fact]
    public async Task BlockUser_ShouldPreventBlockedUserFromLoggingIn()
    {
        await using var app = new AdminApiFactory();
        await app.SeedAsync();
        var client = app.CreateClient();
        await app.AuthorizeAsync(client, "admin@example.com");
        var blockResponse = await client.PatchAsync($"/api/v1/admin/users/{app.UserId}/block", null);
        Assert.Equal(HttpStatusCode.OK, blockResponse.StatusCode);
        client.DefaultRequestHeaders.Authorization = null;

        var loginResponse = await client.PostAsJsonAsync("/api/v1/auth/login", new
        {
            email = "user@example.com",
            password = "Password123!"
        });

        Assert.Equal(HttpStatusCode.Forbidden, loginResponse.StatusCode);
    }

    [Fact]
    public async Task BlockUser_ShouldReturnBadRequest_WhenAdminBlocksSelf()
    {
        await using var app = new AdminApiFactory();
        await app.SeedAsync();
        var client = app.CreateClient();
        await app.AuthorizeAsync(client, "admin@example.com");

        var response = await client.PatchAsync($"/api/v1/admin/users/{app.AdminId}/block", null);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task ListOrders_ShouldReturnAllOrders_WhenAdmin()
    {
        await using var app = new AdminApiFactory();
        await app.SeedAsync();
        var client = app.CreateClient();
        await app.AuthorizeAsync(client, "admin@example.com");

        var response = await client.GetAsync("/api/v1/admin/orders");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var payload = await response.Content.ReadFromJsonAsync<IReadOnlyCollection<AdminOrderPayload>>();
        Assert.NotNull(payload);
        var order = Assert.Single(payload);
        Assert.Equal(app.OrderId, order.Id);
        Assert.Equal(200m, order.TotalAmount);
        Assert.Equal("Paid", order.Status);
        Assert.Equal(2, order.TicketIds.Count);
    }

    [Fact]
    public async Task ApproveEvent_ShouldPublishDraftEvent_WhenAdmin()
    {
        await using var app = new AdminApiFactory();
        await app.SeedAsync();
        var client = app.CreateClient();
        await app.AuthorizeAsync(client, "admin@example.com");

        var response = await client.PatchAsync($"/api/v1/admin/events/{app.EventId}/approve", null);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var payload = await response.Content.ReadFromJsonAsync<AdminEventPayload>();
        Assert.NotNull(payload);
        Assert.Equal(app.EventId, payload.Id);
        Assert.Equal("Published", payload.Status);

        using var scope = app.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<TicketFlowDbContext>();
        var ticketEvent = await dbContext.Events.SingleAsync(ticketEvent => ticketEvent.Id == app.EventId);
        Assert.Equal(EventStatus.Published, ticketEvent.Status);
    }

    private sealed record AdminUserPayload(Guid Id, string Email, string Role, bool IsBlocked);

    private sealed record AdminOrderPayload(
        Guid Id,
        Guid UserId,
        Guid SessionId,
        Guid ReservationId,
        decimal TotalAmount,
        string Currency,
        string Status,
        IReadOnlyCollection<Guid> TicketIds);

    private sealed record AdminEventPayload(
        Guid Id,
        string Title,
        string Description,
        Guid OrganizerId,
        string Status);

    private sealed record AuthPayload(
        Guid UserId,
        string Email,
        string Role,
        string AccessToken,
        string RefreshToken,
        DateTime AccessTokenExpiresAtUtc,
        DateTime RefreshTokenExpiresAtUtc);

    private sealed class AdminApiFactory : WebApplicationFactory<Program>
    {
        private readonly string _databaseName = $"ticketflow-admin-tests-{Guid.NewGuid()}";

        public Guid AdminId { get; } = Guid.NewGuid();

        public Guid OrganizerId { get; } = Guid.NewGuid();

        public Guid UserId { get; } = Guid.NewGuid();

        public Guid HallId { get; } = Guid.NewGuid();

        public Guid EventId { get; } = Guid.NewGuid();

        public Guid SessionId { get; } = Guid.NewGuid();

        public Guid OrderId { get; } = Guid.NewGuid();

        public async Task SeedAsync()
        {
            using var scope = Services.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<TicketFlowDbContext>();
            await dbContext.Database.EnsureDeletedAsync();
            await dbContext.Database.EnsureCreatedAsync();

            dbContext.Users.AddRange(
                new User(AdminId, "admin@example.com", HashPassword("Password123!"), UserRole.Admin),
                new User(OrganizerId, "organizer@example.com", HashPassword("Password123!"), UserRole.Organizer),
                new User(UserId, "user@example.com", HashPassword("Password123!"), UserRole.User));

            var hall = new Hall(HallId, "Main hall", 1, 3);
            var seats = hall.Seats.OrderBy(seat => seat.Number).ToArray();
            dbContext.Halls.Add(hall);

            var ticketEvent = new Event(EventId, "Draft event", "Waiting for approval", OrganizerId);
            var session = new Session(
                SessionId,
                EventId,
                HallId,
                new DateTime(2026, 06, 01, 18, 00, 00, DateTimeKind.Utc),
                new DateTime(2026, 06, 01, 20, 00, 00, DateTimeKind.Utc),
                new Money(100m, "USD"));
            ticketEvent.AddSession(session);
            dbContext.Events.Add(ticketEvent);
            dbContext.Sessions.Add(session);

            var order = new Order(OrderId, UserId, SessionId, Guid.NewGuid(), new Money(200m, "USD"));
            order.MarkPaid();
            foreach (var seatId in seats.Take(2).Select(seat => seat.Id))
            {
                var ticketId = Guid.NewGuid();
                order.AddTicket(new Ticket(ticketId, order.Id, SessionId, seatId, $"ticket:{ticketId:N}"));
            }

            dbContext.Orders.Add(order);

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
