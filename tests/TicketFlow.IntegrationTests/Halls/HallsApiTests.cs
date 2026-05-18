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

namespace TicketFlow.IntegrationTests.Halls;

public sealed class HallsApiTests
{
    [Fact]
    public async Task Create_ShouldGenerateSeats_WhenOrganizerCreatesHall()
    {
        await using var app = new HallsApiFactory();
        await app.SeedAsync();
        var client = app.CreateClient();
        await app.AuthorizeAsync(client, "organizer@example.com");

        var response = await client.PostAsJsonAsync("/api/v1/halls", new
        {
            name = "Balcony hall",
            rows = 3,
            seatsPerRow = 4
        });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var payload = await response.Content.ReadFromJsonAsync<HallPayload>();
        Assert.NotNull(payload);
        Assert.Equal(3, payload.Rows);
        Assert.Equal(4, payload.SeatsPerRow);
        Assert.Equal(12, payload.Seats.Count);
        Assert.Contains(payload.Seats, seat => seat.Row == 3 && seat.Number == 4);
    }

    [Fact]
    public async Task Create_ShouldForbidRegularUser()
    {
        await using var app = new HallsApiFactory();
        await app.SeedAsync();
        var client = app.CreateClient();
        await app.AuthorizeAsync(client, "user@example.com");

        var response = await client.PostAsJsonAsync("/api/v1/halls", new
        {
            name = "Forbidden hall",
            rows = 2,
            seatsPerRow = 2
        });

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task GetById_ShouldReturnHallWithGeneratedSeats()
    {
        await using var app = new HallsApiFactory();
        await app.SeedAsync();
        var client = app.CreateClient();

        var response = await client.GetAsync($"/api/v1/halls/{app.HallId}");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var payload = await response.Content.ReadFromJsonAsync<HallPayload>();
        Assert.NotNull(payload);
        Assert.Equal(app.HallId, payload.Id);
        Assert.Equal(6, payload.Seats.Count);
        Assert.Equal(1, payload.Seats[0].Row);
        Assert.Equal(1, payload.Seats[0].Number);
    }

    [Fact]
    public async Task List_ShouldReturnHallsWithGeneratedSeats()
    {
        await using var app = new HallsApiFactory();
        await app.SeedAsync();
        var client = app.CreateClient();

        var response = await client.GetAsync("/api/v1/halls");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var payload = await response.Content.ReadFromJsonAsync<List<HallPayload>>();
        Assert.NotNull(payload);
        var hall = Assert.Single(payload);
        Assert.Equal(app.HallId, hall.Id);
        Assert.Equal(6, hall.Seats.Count);
    }

    [Fact]
    public async Task SessionSeats_ShouldReturnSeats_ForPublishedEvent()
    {
        await using var app = new HallsApiFactory();
        await app.SeedAsync();
        var client = app.CreateClient();

        var response = await client.GetAsync($"/api/v1/sessions/{app.PublishedSessionId}/seats");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var payload = await response.Content.ReadFromJsonAsync<List<SeatPayload>>();
        Assert.NotNull(payload);
        Assert.Equal(6, payload.Count);
        Assert.All(payload, seat => Assert.Equal(app.HallId, seat.HallId));
    }

    [Fact]
    public async Task SessionSeats_ShouldHideDraftEventFromAnonymousUser()
    {
        await using var app = new HallsApiFactory();
        await app.SeedAsync();
        var client = app.CreateClient();

        var response = await client.GetAsync($"/api/v1/sessions/{app.DraftSessionId}/seats");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task SessionSeats_ShouldAllowOwnerOrganizerForDraftEvent()
    {
        await using var app = new HallsApiFactory();
        await app.SeedAsync();
        var client = app.CreateClient();
        await app.AuthorizeAsync(client, "organizer@example.com");

        var response = await client.GetAsync($"/api/v1/sessions/{app.DraftSessionId}/seats");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var payload = await response.Content.ReadFromJsonAsync<List<SeatPayload>>();
        Assert.NotNull(payload);
        Assert.Equal(6, payload.Count);
    }

    private sealed record HallPayload(
        Guid Id,
        string Name,
        int Rows,
        int SeatsPerRow,
        List<SeatPayload> Seats);

    private sealed record SeatPayload(Guid Id, Guid HallId, int Row, int Number);

    private sealed record AuthPayload(
        Guid UserId,
        string Email,
        string Role,
        string AccessToken,
        string RefreshToken,
        DateTime AccessTokenExpiresAtUtc,
        DateTime RefreshTokenExpiresAtUtc);

    private sealed class HallsApiFactory : WebApplicationFactory<Program>
    {
        private readonly string _databaseName = $"ticketflow-halls-tests-{Guid.NewGuid()}";

        public Guid OrganizerId { get; } = Guid.NewGuid();

        public Guid UserId { get; } = Guid.NewGuid();

        public Guid HallId { get; } = Guid.NewGuid();

        public Guid DraftEventId { get; } = Guid.NewGuid();

        public Guid PublishedEventId { get; } = Guid.NewGuid();

        public Guid DraftSessionId { get; } = Guid.NewGuid();

        public Guid PublishedSessionId { get; } = Guid.NewGuid();

        public async Task SeedAsync()
        {
            using var scope = Services.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<TicketFlowDbContext>();
            await dbContext.Database.EnsureDeletedAsync();
            await dbContext.Database.EnsureCreatedAsync();

            dbContext.Users.AddRange(
                new User(OrganizerId, "organizer@example.com", HashPassword("Password123!"), UserRole.Organizer),
                new User(UserId, "user@example.com", HashPassword("Password123!"), UserRole.User));

            dbContext.Halls.Add(new Hall(HallId, "Main hall", 2, 3));

            var draftEvent = new Event(DraftEventId, "Draft event", "Not visible publicly", OrganizerId);
            var publishedEvent = new Event(PublishedEventId, "Published event", "Visible publicly", OrganizerId);
            publishedEvent.ChangeStatus(EventStatus.Published);

            var draftSession = new Session(
                DraftSessionId,
                DraftEventId,
                HallId,
                new DateTime(2026, 06, 01, 18, 00, 00, DateTimeKind.Utc),
                new DateTime(2026, 06, 01, 20, 00, 00, DateTimeKind.Utc),
                new Money(1000, "USD"));
            var publishedSession = new Session(
                PublishedSessionId,
                PublishedEventId,
                HallId,
                new DateTime(2026, 06, 02, 18, 00, 00, DateTimeKind.Utc),
                new DateTime(2026, 06, 02, 20, 00, 00, DateTimeKind.Utc),
                new Money(1000, "USD"));

            draftEvent.AddSession(draftSession);
            publishedEvent.AddSession(publishedSession);

            dbContext.Events.AddRange(draftEvent, publishedEvent);
            dbContext.Sessions.AddRange(draftSession, publishedSession);

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
            builder.UseSetting("Jwt:Issuer", "TicketFlow.Tests");
            builder.UseSetting("Jwt:Audience", "TicketFlow.Tests");
            builder.UseSetting("Jwt:SigningKey", "ticketflow-tests-signing-key-with-32-plus-chars");

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
