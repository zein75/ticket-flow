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

namespace TicketFlow.IntegrationTests.SeatMaps;

public sealed class SeatMapsApiTests
{
    [Fact]
    public async Task SeatMap_ShouldReturnAvailableReservedAndSoldStatuses()
    {
        await using var app = new SeatMapsApiFactory();
        await app.SeedAsync();
        var client = app.CreateClient();

        var response = await client.GetAsync($"/api/v1/sessions/{app.PublishedSessionId}/seat-map");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var payload = await response.Content.ReadFromJsonAsync<SeatMapPayload>();
        Assert.NotNull(payload);
        Assert.Equal(app.PublishedSessionId, payload.SessionId);
        Assert.Equal(4, payload.Seats.Count);
        Assert.Contains(payload.Seats, seat => seat.SeatId == app.AvailableSeatId && seat.Status == "Available");
        Assert.Contains(payload.Seats, seat => seat.SeatId == app.ReservedSeatId && seat.Status == "Reserved");
        Assert.Contains(payload.Seats, seat => seat.SeatId == app.SoldSeatId && seat.Status == "Sold");
    }

    [Fact]
    public async Task SeatMap_ShouldHideDraftEventFromAnonymousUser()
    {
        await using var app = new SeatMapsApiFactory();
        await app.SeedAsync();
        var client = app.CreateClient();

        var response = await client.GetAsync($"/api/v1/sessions/{app.DraftSessionId}/seat-map");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task SeatMap_ShouldAllowOwnerOrganizerForDraftEvent()
    {
        await using var app = new SeatMapsApiFactory();
        await app.SeedAsync();
        var client = app.CreateClient();
        await app.AuthorizeAsync(client, "organizer@example.com");

        var response = await client.GetAsync($"/api/v1/sessions/{app.DraftSessionId}/seat-map");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var payload = await response.Content.ReadFromJsonAsync<SeatMapPayload>();
        Assert.NotNull(payload);
        Assert.Equal(4, payload.Seats.Count);
        Assert.All(payload.Seats, seat => Assert.Equal("Available", seat.Status));
    }

    [Fact]
    public async Task SeatMap_ShouldIgnoreExpiredReservations()
    {
        await using var app = new SeatMapsApiFactory();
        await app.SeedAsync(includeExpiredReservation: true);
        var client = app.CreateClient();

        var response = await client.GetAsync($"/api/v1/sessions/{app.PublishedSessionId}/seat-map");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var payload = await response.Content.ReadFromJsonAsync<SeatMapPayload>();
        Assert.NotNull(payload);
        Assert.Contains(payload.Seats, seat => seat.SeatId == app.ExpiredReservationSeatId && seat.Status == "Available");
    }

    private sealed record SeatMapPayload(Guid SessionId, List<SeatMapItemPayload> Seats);

    private sealed record SeatMapItemPayload(Guid SeatId, int Row, int Number, string Status);

    private sealed record AuthPayload(
        Guid UserId,
        string Email,
        string Role,
        string AccessToken,
        string RefreshToken,
        DateTime AccessTokenExpiresAtUtc,
        DateTime RefreshTokenExpiresAtUtc);

    private sealed class SeatMapsApiFactory : WebApplicationFactory<Program>
    {
        private readonly string _databaseName = $"ticketflow-seat-map-tests-{Guid.NewGuid()}";

        public Guid OrganizerId { get; } = Guid.NewGuid();

        public Guid UserId { get; } = Guid.NewGuid();

        public Guid HallId { get; } = Guid.NewGuid();

        public Guid PublishedEventId { get; } = Guid.NewGuid();

        public Guid DraftEventId { get; } = Guid.NewGuid();

        public Guid PublishedSessionId { get; } = Guid.NewGuid();

        public Guid DraftSessionId { get; } = Guid.NewGuid();

        public Guid AvailableSeatId { get; private set; }

        public Guid ReservedSeatId { get; private set; }

        public Guid SoldSeatId { get; private set; }

        public Guid ExpiredReservationSeatId { get; private set; }

        public async Task SeedAsync(bool includeExpiredReservation = false)
        {
            using var scope = Services.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<TicketFlowDbContext>();
            await dbContext.Database.EnsureDeletedAsync();
            await dbContext.Database.EnsureCreatedAsync();

            var user = new User(UserId, "user@example.com", HashPassword("Password123!"), UserRole.User);
            dbContext.Users.AddRange(
                new User(OrganizerId, "organizer@example.com", HashPassword("Password123!"), UserRole.Organizer),
                user);

            var hall = new Hall(HallId, "Main hall", 2, 2);
            var seats = hall.Seats.OrderBy(seat => seat.Row).ThenBy(seat => seat.Number).ToArray();
            AvailableSeatId = seats[0].Id;
            ReservedSeatId = seats[1].Id;
            SoldSeatId = seats[2].Id;
            ExpiredReservationSeatId = seats[3].Id;
            dbContext.Halls.Add(hall);

            var publishedEvent = new Event(PublishedEventId, "Published event", "Visible publicly", OrganizerId);
            publishedEvent.ChangeStatus(EventStatus.Published);
            var draftEvent = new Event(DraftEventId, "Draft event", "Not visible publicly", OrganizerId);

            var publishedSession = new Session(
                PublishedSessionId,
                PublishedEventId,
                HallId,
                new DateTime(2026, 06, 01, 18, 00, 00, DateTimeKind.Utc),
                new DateTime(2026, 06, 01, 20, 00, 00, DateTimeKind.Utc),
                new Money(1000, "USD"));
            var draftSession = new Session(
                DraftSessionId,
                DraftEventId,
                HallId,
                new DateTime(2026, 06, 02, 18, 00, 00, DateTimeKind.Utc),
                new DateTime(2026, 06, 02, 20, 00, 00, DateTimeKind.Utc),
                new Money(1000, "USD"));

            publishedEvent.AddSession(publishedSession);
            draftEvent.AddSession(draftSession);
            dbContext.Events.AddRange(publishedEvent, draftEvent);
            dbContext.Sessions.AddRange(publishedSession, draftSession);

            var reservedReservation = new Reservation(
                Guid.NewGuid(),
                UserId,
                PublishedSessionId,
                DateTime.UtcNow.AddMinutes(10),
                [ReservedSeatId]);
            dbContext.Reservations.Add(reservedReservation);

            var soldReservation = new Reservation(
                Guid.NewGuid(),
                UserId,
                PublishedSessionId,
                DateTime.UtcNow.AddMinutes(10),
                [SoldSeatId]);
            soldReservation.ConvertToOrder(DateTime.UtcNow);
            var order = new Order(Guid.NewGuid(), UserId, PublishedSessionId, soldReservation.Id, new Money(1000, "USD"));
            order.MarkPaid();
            var ticket = new Ticket(Guid.NewGuid(), order.Id, PublishedSessionId, SoldSeatId, "qr-payload");
            order.AddTicket(ticket);
            dbContext.Reservations.Add(soldReservation);
            dbContext.Orders.Add(order);
            dbContext.Tickets.Add(ticket);

            if (includeExpiredReservation)
            {
                var expiredReservation = new Reservation(
                    Guid.NewGuid(),
                    UserId,
                    PublishedSessionId,
                    DateTime.UtcNow.AddMinutes(-10),
                    [ExpiredReservationSeatId]);
                dbContext.Reservations.Add(expiredReservation);
            }

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
