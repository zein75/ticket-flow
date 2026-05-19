using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text.Json.Nodes;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using TicketFlow.Application.Abstractions.QrCodes;
using TicketFlow.Domain.Entities;
using TicketFlow.Domain.Enums;
using TicketFlow.Domain.ValueObjects;
using TicketFlow.Infrastructure.Persistence;

namespace TicketFlow.IntegrationTests.Tickets;

public sealed class TicketsApiTests
{
    [Fact]
    public async Task ListMine_ShouldReturnOnlyCurrentUsersTickets()
    {
        await using var app = new TicketsApiFactory();
        await app.SeedAsync();
        var client = app.CreateClient();
        await app.AuthorizeAsync(client, "user@example.com");

        var response = await client.GetAsync("/api/v1/tickets/my");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var payload = await response.Content.ReadFromJsonAsync<IReadOnlyCollection<TicketPayload>>();
        Assert.NotNull(payload);
        var ticket = Assert.Single(payload);
        Assert.Equal(app.UserTicketId, ticket.Id);
        Assert.Equal(app.UserOrderId, ticket.OrderId);
    }

    [Fact]
    public async Task GetById_ShouldReturnTicket_WhenUserOwnsTicket()
    {
        await using var app = new TicketsApiFactory();
        await app.SeedAsync();
        var client = app.CreateClient();
        await app.AuthorizeAsync(client, "user@example.com");

        var response = await client.GetAsync($"/api/v1/tickets/{app.UserTicketId}");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var payload = await response.Content.ReadFromJsonAsync<TicketPayload>();
        Assert.NotNull(payload);
        Assert.Equal(app.UserTicketId, payload.Id);
        Assert.Equal("Active", payload.Status);
    }

    [Fact]
    public async Task GetById_ShouldReturnForbidden_WhenTicketBelongsToAnotherUser()
    {
        await using var app = new TicketsApiFactory();
        await app.SeedAsync();
        var client = app.CreateClient();
        await app.AuthorizeAsync(client, "other-user@example.com");

        var response = await client.GetAsync($"/api/v1/tickets/{app.UserTicketId}");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task GetQrCode_ShouldReturnPng_WhenUserOwnsTicket()
    {
        await using var app = new TicketsApiFactory();
        await app.SeedAsync();
        var client = app.CreateClient();
        await app.AuthorizeAsync(client, "user@example.com");

        var response = await client.GetAsync($"/api/v1/tickets/{app.UserTicketId}/qr");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("image/png", response.Content.Headers.ContentType?.MediaType);
        var image = await response.Content.ReadAsByteArrayAsync();
        Assert.True(image.Length > 8);
        Assert.Equal([0x89, 0x50, 0x4E, 0x47], image[..4]);
    }

    [Fact]
    public async Task Validate_ShouldReturnValid_WhenOrganizerOwnsEvent()
    {
        await using var app = new TicketsApiFactory();
        await app.SeedAsync();
        var client = app.CreateClient();
        await app.AuthorizeAsync(client, "organizer@example.com");

        var response = await client.PostAsJsonAsync("/api/v1/tickets/validate", new
        {
            payload = app.UserTicketPayload
        });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var payload = await response.Content.ReadFromJsonAsync<TicketValidationPayload>();
        Assert.NotNull(payload);
        Assert.Equal(app.UserTicketId, payload.TicketId);
        Assert.True(payload.IsValid);
        Assert.Equal("Active", payload.Status);
    }

    [Fact]
    public async Task Validate_ShouldReturnForbidden_WhenOrganizerDoesNotOwnEvent()
    {
        await using var app = new TicketsApiFactory();
        await app.SeedAsync();
        var client = app.CreateClient();
        await app.AuthorizeAsync(client, "other-organizer@example.com");

        var response = await client.PostAsJsonAsync("/api/v1/tickets/validate", new
        {
            payload = app.UserTicketPayload
        });

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Validate_ShouldReturnBadRequest_WhenPayloadSignatureIsInvalid()
    {
        await using var app = new TicketsApiFactory();
        await app.SeedAsync();
        var client = app.CreateClient();
        await app.AuthorizeAsync(client, "organizer@example.com");
        var payload = JsonNode.Parse(app.UserTicketPayload)!.AsObject();
        payload["signature"] = "invalid-signature";

        var response = await client.PostAsJsonAsync("/api/v1/tickets/validate", new
        {
            payload = payload.ToJsonString()
        });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    private sealed record TicketPayload(Guid Id, Guid OrderId, Guid SessionId, Guid SeatId, string Status);

    private sealed record TicketValidationPayload(Guid TicketId, Guid OrderId, string Status, bool IsValid);

    private sealed record AuthPayload(
        Guid UserId,
        string Email,
        string Role,
        string AccessToken,
        string RefreshToken,
        DateTime AccessTokenExpiresAtUtc,
        DateTime RefreshTokenExpiresAtUtc);

    private sealed class TicketsApiFactory : WebApplicationFactory<Program>
    {
        private readonly string _databaseName = $"ticketflow-ticket-tests-{Guid.NewGuid()}";

        public Guid UserId { get; } = Guid.NewGuid();

        public Guid OtherUserId { get; } = Guid.NewGuid();

        public Guid OrganizerId { get; } = Guid.NewGuid();

        public Guid OtherOrganizerId { get; } = Guid.NewGuid();

        public Guid HallId { get; } = Guid.NewGuid();

        public Guid EventId { get; } = Guid.NewGuid();

        public Guid OtherEventId { get; } = Guid.NewGuid();

        public Guid SessionId { get; } = Guid.NewGuid();

        public Guid OtherSessionId { get; } = Guid.NewGuid();

        public Guid UserOrderId { get; } = Guid.NewGuid();

        public Guid OtherUserOrderId { get; } = Guid.NewGuid();

        public Guid UserTicketId { get; } = Guid.NewGuid();

        public Guid OtherUserTicketId { get; } = Guid.NewGuid();

        public string UserTicketPayload { get; private set; } = string.Empty;

        public async Task SeedAsync()
        {
            using var scope = Services.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<TicketFlowDbContext>();
            var payloads = scope.ServiceProvider.GetRequiredService<IQrCodePayloadProtector>();
            await dbContext.Database.EnsureDeletedAsync();
            await dbContext.Database.EnsureCreatedAsync();

            dbContext.Users.AddRange(
                new User(UserId, "user@example.com", HashPassword("Password123!"), UserRole.User),
                new User(OtherUserId, "other-user@example.com", HashPassword("Password123!"), UserRole.User),
                new User(OrganizerId, "organizer@example.com", HashPassword("Password123!"), UserRole.Organizer),
                new User(OtherOrganizerId, "other-organizer@example.com", HashPassword("Password123!"), UserRole.Organizer));

            var hall = new Hall(HallId, "Main hall", 1, 2);
            var seats = hall.Seats.OrderBy(seat => seat.Number).ToArray();
            dbContext.Halls.Add(hall);

            var ticketEvent = new Event(EventId, "Published event", "Visible publicly", OrganizerId);
            ticketEvent.ChangeStatus(EventStatus.Published);
            var session = new Session(
                SessionId,
                EventId,
                HallId,
                new DateTime(2026, 06, 01, 18, 00, 00, DateTimeKind.Utc),
                new DateTime(2026, 06, 01, 20, 00, 00, DateTimeKind.Utc),
                new Money(1000, "USD"));
            ticketEvent.AddSession(session);
            dbContext.Events.Add(ticketEvent);
            dbContext.Sessions.Add(session);

            var otherEvent = new Event(OtherEventId, "Other event", "Other organizer event", OtherOrganizerId);
            otherEvent.ChangeStatus(EventStatus.Published);
            var otherSession = new Session(
                OtherSessionId,
                OtherEventId,
                HallId,
                new DateTime(2026, 06, 02, 18, 00, 00, DateTimeKind.Utc),
                new DateTime(2026, 06, 02, 20, 00, 00, DateTimeKind.Utc),
                new Money(1000, "USD"));
            otherEvent.AddSession(otherSession);
            dbContext.Events.Add(otherEvent);
            dbContext.Sessions.Add(otherSession);

            UserTicketPayload = payloads.CreatePayload(UserTicketId, UserOrderId);
            var userOrder = new Order(UserOrderId, UserId, SessionId, Guid.NewGuid(), new Money(1000, "USD"));
            userOrder.MarkPaid();
            userOrder.AddTicket(new Ticket(UserTicketId, UserOrderId, SessionId, seats[0].Id, UserTicketPayload));

            var otherUserTicketPayload = payloads.CreatePayload(OtherUserTicketId, OtherUserOrderId);
            var otherUserOrder = new Order(OtherUserOrderId, OtherUserId, SessionId, Guid.NewGuid(), new Money(1000, "USD"));
            otherUserOrder.MarkPaid();
            otherUserOrder.AddTicket(new Ticket(OtherUserTicketId, OtherUserOrderId, SessionId, seats[1].Id, otherUserTicketPayload));

            dbContext.Orders.AddRange(userOrder, otherUserOrder);

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
