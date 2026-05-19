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

namespace TicketFlow.IntegrationTests.Payments;

public sealed class ReservationPaymentsApiTests
{
    [Fact]
    public async Task Pay_ShouldReturnSucceeded_WhenMockProviderApprovesCard()
    {
        await using var app = new ReservationPaymentsApiFactory();
        await app.SeedAsync();
        var client = app.CreateClient();
        await app.AuthorizeAsync(client, "user@example.com");

        var response = await client.PostAsJsonAsync($"/api/v1/reservations/{app.ActiveReservationId}/pay", new
        {
            cardNumber = "4111111111111111"
        });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var payload = await response.Content.ReadFromJsonAsync<PaymentPayload>();
        Assert.NotNull(payload);
        Assert.True(payload.Succeeded);
        Assert.Equal("Succeeded", payload.Status);
        Assert.False(string.IsNullOrWhiteSpace(payload.ProviderPaymentId));
        Assert.Null(payload.FailureReason);
    }

    [Fact]
    public async Task Pay_ShouldReturnFailed_WhenCardEndsWith0000()
    {
        await using var app = new ReservationPaymentsApiFactory();
        await app.SeedAsync();
        var client = app.CreateClient();
        await app.AuthorizeAsync(client, "user@example.com");

        var response = await client.PostAsJsonAsync($"/api/v1/reservations/{app.ActiveReservationId}/pay", new
        {
            cardNumber = "4111111111110000"
        });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var payload = await response.Content.ReadFromJsonAsync<PaymentPayload>();
        Assert.NotNull(payload);
        Assert.False(payload.Succeeded);
        Assert.Equal("Failed", payload.Status);
        Assert.Null(payload.ProviderPaymentId);
        Assert.False(string.IsNullOrWhiteSpace(payload.FailureReason));
    }

    [Fact]
    public async Task Pay_ShouldReturnForbidden_WhenReservationBelongsToAnotherUser()
    {
        await using var app = new ReservationPaymentsApiFactory();
        await app.SeedAsync();
        var client = app.CreateClient();
        await app.AuthorizeAsync(client, "other-user@example.com");

        var response = await client.PostAsJsonAsync($"/api/v1/reservations/{app.ActiveReservationId}/pay", new
        {
            cardNumber = "4111111111111111"
        });

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Pay_ShouldReturnConflict_WhenReservationIsExpired()
    {
        await using var app = new ReservationPaymentsApiFactory();
        await app.SeedAsync();
        var client = app.CreateClient();
        await app.AuthorizeAsync(client, "user@example.com");

        var response = await client.PostAsJsonAsync($"/api/v1/reservations/{app.ExpiredReservationId}/pay", new
        {
            cardNumber = "4111111111111111"
        });

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    private sealed record PaymentPayload(
        Guid ReservationId,
        bool Succeeded,
        string Status,
        string? ProviderPaymentId,
        string? FailureReason);

    private sealed record AuthPayload(
        Guid UserId,
        string Email,
        string Role,
        string AccessToken,
        string RefreshToken,
        DateTime AccessTokenExpiresAtUtc,
        DateTime RefreshTokenExpiresAtUtc);

    private sealed class ReservationPaymentsApiFactory : WebApplicationFactory<Program>
    {
        private readonly string _databaseName = $"ticketflow-payment-tests-{Guid.NewGuid()}";

        public Guid UserId { get; } = Guid.NewGuid();

        public Guid OtherUserId { get; } = Guid.NewGuid();

        public Guid OrganizerId { get; } = Guid.NewGuid();

        public Guid HallId { get; } = Guid.NewGuid();

        public Guid EventId { get; } = Guid.NewGuid();

        public Guid SessionId { get; } = Guid.NewGuid();

        public Guid ActiveReservationId { get; } = Guid.NewGuid();

        public Guid ExpiredReservationId { get; } = Guid.NewGuid();

        public async Task SeedAsync()
        {
            using var scope = Services.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<TicketFlowDbContext>();
            await dbContext.Database.EnsureDeletedAsync();
            await dbContext.Database.EnsureCreatedAsync();

            dbContext.Users.AddRange(
                new User(UserId, "user@example.com", HashPassword("Password123!"), UserRole.User),
                new User(OtherUserId, "other-user@example.com", HashPassword("Password123!"), UserRole.User),
                new User(OrganizerId, "organizer@example.com", HashPassword("Password123!"), UserRole.Organizer));

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

            dbContext.Reservations.AddRange(
                new Reservation(
                    ActiveReservationId,
                    UserId,
                    SessionId,
                    DateTime.UtcNow.AddMinutes(10),
                    [seats[0].Id]),
                new Reservation(
                    ExpiredReservationId,
                    UserId,
                    SessionId,
                    DateTime.UtcNow.AddMinutes(-10),
                    [seats[1].Id]));

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
