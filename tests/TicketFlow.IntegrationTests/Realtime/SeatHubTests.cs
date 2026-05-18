using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Cryptography;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using TicketFlow.Application.Abstractions.Reservations;
using TicketFlow.Domain.Entities;
using TicketFlow.Domain.Enums;
using TicketFlow.Domain.ValueObjects;
using TicketFlow.Infrastructure.Persistence;

namespace TicketFlow.IntegrationTests.Realtime;

public sealed class SeatHubTests
{
    [Fact]
    public async Task Reservation_ShouldPublishSeatReserved_ToJoinedSessionGroup()
    {
        await using var app = new SeatHubApiFactory();
        await app.SeedAsync();
        var client = app.CreateClient();
        await app.AuthorizeAsync(client);
        var receivedEvent = new TaskCompletionSource<SeatReservedPayload>(TaskCreationOptions.RunContinuationsAsynchronously);

        await using var connection = new HubConnectionBuilder()
            .WithUrl(
                new Uri(client.BaseAddress!, "/api/v1/hubs/seats"),
                options =>
                {
                    options.HttpMessageHandlerFactory = _ => app.Server.CreateHandler();
                })
            .Build();
        connection.On<SeatReservedPayload>("SeatReserved", payload => receivedEvent.TrySetResult(payload));
        await connection.StartAsync();
        await connection.InvokeAsync("JoinSessionGroup", app.SessionId);

        var reservationResponse = await client.PostAsJsonAsync("/api/v1/reservations", new
        {
            sessionId = app.SessionId,
            seatIds = new[] { app.FirstSeatId }
        });

        Assert.Equal(HttpStatusCode.Created, reservationResponse.StatusCode);
        var completedTask = await Task.WhenAny(receivedEvent.Task, Task.Delay(TimeSpan.FromSeconds(5)));
        Assert.Same(receivedEvent.Task, completedTask);
        var payload = await receivedEvent.Task;
        Assert.Equal(app.SessionId, payload.SessionId);
        Assert.Contains(app.FirstSeatId, payload.SeatIds);
    }

    private sealed record SeatReservedPayload(Guid SessionId, Guid ReservationId, List<Guid> SeatIds);

    private sealed record AuthPayload(
        Guid UserId,
        string Email,
        string Role,
        string AccessToken,
        string RefreshToken,
        DateTime AccessTokenExpiresAtUtc,
        DateTime RefreshTokenExpiresAtUtc);

    private sealed class SeatHubApiFactory : WebApplicationFactory<Program>
    {
        private readonly string _databaseName = $"ticketflow-realtime-tests-{Guid.NewGuid()}";

        public Guid UserId { get; } = Guid.NewGuid();

        public Guid OrganizerId { get; } = Guid.NewGuid();

        public Guid HallId { get; } = Guid.NewGuid();

        public Guid EventId { get; } = Guid.NewGuid();

        public Guid SessionId { get; } = Guid.NewGuid();

        public Guid FirstSeatId { get; private set; }

        public async Task SeedAsync()
        {
            using var scope = Services.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<TicketFlowDbContext>();
            await dbContext.Database.EnsureDeletedAsync();
            await dbContext.Database.EnsureCreatedAsync();

            dbContext.Users.AddRange(
                new User(UserId, "user@example.com", HashPassword("Password123!"), UserRole.User),
                new User(OrganizerId, "organizer@example.com", HashPassword("Password123!"), UserRole.Organizer));

            var hall = new Hall(HallId, "Main hall", 1, 1);
            FirstSeatId = hall.Seats.Single().Id;
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

            await dbContext.SaveChangesAsync();
        }

        public async Task AuthorizeAsync(HttpClient client)
        {
            var response = await client.PostAsJsonAsync("/api/v1/auth/login", new
            {
                email = "user@example.com",
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
            builder.UseSetting("Reservations:TtlMinutes", "10");

            builder.ConfigureServices(services =>
            {
                services.AddLogging(logging => logging.ClearProviders());
                services.RemoveAll<DbContextOptions<TicketFlowDbContext>>();
                services.RemoveAll<IDbContextOptionsConfiguration<TicketFlowDbContext>>();
                services.RemoveAll<ISeatLockStore>();
                services.AddDbContext<TicketFlowDbContext>(options =>
                {
                    options.UseInMemoryDatabase(_databaseName);
                });
                services.AddSingleton<ISeatLockStore, InMemorySeatLockStore>();
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

    private sealed class InMemorySeatLockStore : ISeatLockStore
    {
        public Task<bool> TryLockSeatsAsync(
            Guid sessionId,
            Guid reservationId,
            IReadOnlyCollection<Guid> seatIds,
            TimeSpan ttl,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult(true);
        }

        public Task ReleaseSeatsAsync(
            Guid sessionId,
            Guid reservationId,
            IReadOnlyCollection<Guid> seatIds,
            CancellationToken cancellationToken = default)
        {
            return Task.CompletedTask;
        }
    }
}
