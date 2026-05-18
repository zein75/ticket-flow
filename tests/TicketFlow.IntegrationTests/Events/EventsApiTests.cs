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
using TicketFlow.Infrastructure.Persistence;

namespace TicketFlow.IntegrationTests.Events;

public sealed class EventsApiTests
{
    [Fact]
    public async Task List_ShouldReturnOnlyPublishedEvents_ForAnonymousUser()
    {
        await using var app = new EventsApiFactory();
        await app.SeedAsync();
        var client = app.CreateClient();

        var response = await client.GetAsync("/api/v1/events");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var events = await response.Content.ReadFromJsonAsync<List<EventPayload>>();
        Assert.NotNull(events);
        Assert.Single(events);
        Assert.Equal(app.PublishedEventId, events[0].Id);
        Assert.Equal("Published event", events[0].Title);
    }

    [Fact]
    public async Task Create_ShouldAllowOrganizer()
    {
        await using var app = new EventsApiFactory();
        await app.SeedAsync();
        var client = app.CreateClient();
        await app.AuthorizeAsync(client, "organizer@example.com");

        var response = await client.PostAsJsonAsync("/api/v1/events", new
        {
            title = "Created by organizer",
            description = "Organizer owned event",
            status = EventStatus.Published
        });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var payload = await response.Content.ReadFromJsonAsync<EventPayload>();
        Assert.NotNull(payload);
        Assert.Equal(app.OrganizerId, payload.OrganizerId);
        Assert.Equal("Published", payload.Status);
    }

    [Fact]
    public async Task Create_ShouldForbidRegularUser()
    {
        await using var app = new EventsApiFactory();
        await app.SeedAsync();
        var client = app.CreateClient();
        await app.AuthorizeAsync(client, "user@example.com");

        var response = await client.PostAsJsonAsync("/api/v1/events", new
        {
            title = "Forbidden event",
            description = "Regular users cannot manage events",
            status = EventStatus.Published
        });

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Update_ShouldForbidAnotherOrganizerEvent()
    {
        await using var app = new EventsApiFactory();
        await app.SeedAsync();
        var client = app.CreateClient();
        await app.AuthorizeAsync(client, "other-organizer@example.com");

        var response = await client.PutAsJsonAsync($"/api/v1/events/{app.DraftEventId}", new
        {
            title = "Attempted update",
            description = "Should be denied",
            status = EventStatus.Published
        });

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Admin_ShouldUpdateOrganizerEventAndCreateSession()
    {
        await using var app = new EventsApiFactory();
        await app.SeedAsync();
        var client = app.CreateClient();
        await app.AuthorizeAsync(client, "admin@example.com");

        var updateResponse = await client.PutAsJsonAsync($"/api/v1/events/{app.DraftEventId}", new
        {
            title = "Admin published event",
            description = "Published by admin",
            status = EventStatus.Published
        });

        Assert.Equal(HttpStatusCode.OK, updateResponse.StatusCode);

        var createSessionResponse = await client.PostAsJsonAsync($"/api/v1/events/{app.DraftEventId}/sessions", new
        {
            hallId = app.HallId,
            startsAtUtc = new DateTime(2026, 06, 01, 18, 00, 00, DateTimeKind.Utc),
            endsAtUtc = new DateTime(2026, 06, 01, 20, 00, 00, DateTimeKind.Utc),
            priceAmount = 1500m,
            priceCurrency = "USD"
        });

        Assert.Equal(HttpStatusCode.Created, createSessionResponse.StatusCode);

        var sessionsResponse = await client.GetAsync($"/api/v1/events/{app.DraftEventId}/sessions");
        Assert.Equal(HttpStatusCode.OK, sessionsResponse.StatusCode);
        var sessions = await sessionsResponse.Content.ReadFromJsonAsync<List<SessionPayload>>();
        Assert.NotNull(sessions);
        Assert.Single(sessions);
        Assert.Equal(app.HallId, sessions[0].HallId);
    }

    private sealed record EventPayload(Guid Id, string Title, string Description, Guid OrganizerId, string Status);

    private sealed record SessionPayload(
        Guid Id,
        Guid EventId,
        Guid HallId,
        DateTime StartsAtUtc,
        DateTime EndsAtUtc,
        decimal PriceAmount,
        string PriceCurrency);

    private sealed record AuthPayload(
        Guid UserId,
        string Email,
        string Role,
        string AccessToken,
        string RefreshToken,
        DateTime AccessTokenExpiresAtUtc,
        DateTime RefreshTokenExpiresAtUtc);

    private sealed class EventsApiFactory : WebApplicationFactory<Program>
    {
        private readonly string _databaseName = $"ticketflow-events-tests-{Guid.NewGuid()}";

        public Guid OrganizerId { get; } = Guid.NewGuid();

        public Guid OtherOrganizerId { get; } = Guid.NewGuid();

        public Guid AdminId { get; } = Guid.NewGuid();

        public Guid UserId { get; } = Guid.NewGuid();

        public Guid HallId { get; } = Guid.NewGuid();

        public Guid DraftEventId { get; } = Guid.NewGuid();

        public Guid PublishedEventId { get; } = Guid.NewGuid();

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

            dbContext.Halls.Add(new Hall(HallId, "Main hall", 2, 5));

            var draftEvent = new Event(DraftEventId, "Draft event", "Not visible publicly", OrganizerId);
            var publishedEvent = new Event(PublishedEventId, "Published event", "Visible publicly", OrganizerId);
            publishedEvent.ChangeStatus(EventStatus.Published);
            dbContext.Events.AddRange(draftEvent, publishedEvent);

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
