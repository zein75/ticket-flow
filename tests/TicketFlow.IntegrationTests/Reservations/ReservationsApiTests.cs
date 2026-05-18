using System.Collections.Concurrent;
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
using TicketFlow.Application.Abstractions.Realtime;
using TicketFlow.Application.Abstractions.Reservations;
using TicketFlow.Domain.Entities;
using TicketFlow.Domain.Enums;
using TicketFlow.Domain.ValueObjects;
using TicketFlow.Infrastructure.Persistence;

namespace TicketFlow.IntegrationTests.Reservations;

public sealed class ReservationsApiTests
{
    [Fact]
    public async Task Create_ShouldReserveSeatsAndReturnExpiration()
    {
        await using var app = new ReservationsApiFactory();
        await app.SeedAsync();
        var client = app.CreateClient();
        await app.AuthorizeAsync(client);

        var response = await client.PostAsJsonAsync("/api/v1/reservations", new
        {
            sessionId = app.SessionId,
            seatIds = new[] { app.FirstSeatId, app.SecondSeatId }
        });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var payload = await response.Content.ReadFromJsonAsync<ReservationPayload>();
        Assert.NotNull(payload);
        Assert.Equal(app.UserId, payload.UserId);
        Assert.Equal(app.SessionId, payload.SessionId);
        Assert.Equal("Active", payload.Status);
        Assert.Equal(2, payload.SeatIds.Count);
        Assert.True(payload.ExpiresAtUtc > DateTime.UtcNow.AddMinutes(9));
    }

    [Fact]
    public async Task Create_ShouldReturnConflict_WhenSeatIsAlreadyReservedInPostgres()
    {
        await using var app = new ReservationsApiFactory();
        await app.SeedAsync();
        var client = app.CreateClient();
        await app.AuthorizeAsync(client);

        var firstResponse = await client.PostAsJsonAsync("/api/v1/reservations", new
        {
            sessionId = app.SessionId,
            seatIds = new[] { app.FirstSeatId }
        });
        Assert.Equal(HttpStatusCode.Created, firstResponse.StatusCode);

        var secondResponse = await client.PostAsJsonAsync("/api/v1/reservations", new
        {
            sessionId = app.SessionId,
            seatIds = new[] { app.FirstSeatId }
        });

        Assert.Equal(HttpStatusCode.Conflict, secondResponse.StatusCode);
    }

    [Fact]
    public async Task Create_ShouldAllowOnlyOneReservation_WhenTwoUsersReserveSameSeatConcurrently()
    {
        await using var app = new ReservationsApiFactory();
        await app.SeedAsync();
        var firstClient = app.CreateClient();
        var secondClient = app.CreateClient();
        await app.AuthorizeAsync(firstClient, "user@example.com");
        await app.AuthorizeAsync(secondClient, "second-user@example.com");

        var firstRequest = firstClient.PostAsJsonAsync("/api/v1/reservations", new
        {
            sessionId = app.SessionId,
            seatIds = new[] { app.FirstSeatId }
        });
        var secondRequest = secondClient.PostAsJsonAsync("/api/v1/reservations", new
        {
            sessionId = app.SessionId,
            seatIds = new[] { app.FirstSeatId }
        });

        var responses = await Task.WhenAll(firstRequest, secondRequest);
        var statuses = responses.Select(response => response.StatusCode).Order().ToArray();

        Assert.Equal([HttpStatusCode.Created, HttpStatusCode.Conflict], statuses);
        Assert.Equal(1, await app.CountActiveReservationsAsync(app.FirstSeatId));
    }

    [Fact]
    public async Task Create_ShouldReturnConflict_WhenSeatIsAlreadyLockedInRedis()
    {
        var lockStore = new InMemorySeatLockStore();
        await using var app = new ReservationsApiFactory(lockStore);
        await app.SeedAsync();
        await lockStore.TryLockSeatsAsync(app.SessionId, Guid.NewGuid(), [app.FirstSeatId], TimeSpan.FromMinutes(10));
        var client = app.CreateClient();
        await app.AuthorizeAsync(client, "user@example.com");

        var response = await client.PostAsJsonAsync("/api/v1/reservations", new
        {
            sessionId = app.SessionId,
            seatIds = new[] { app.FirstSeatId }
        });

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    [Fact]
    public async Task Create_ShouldReturnNotFound_WhenSeatDoesNotBelongToSessionHall()
    {
        await using var app = new ReservationsApiFactory();
        await app.SeedAsync();
        var client = app.CreateClient();
        await app.AuthorizeAsync(client, "user@example.com");

        var response = await client.PostAsJsonAsync("/api/v1/reservations", new
        {
            sessionId = app.SessionId,
            seatIds = new[] { Guid.NewGuid() }
        });

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task SeatMap_ShouldShowReservedAfterReservation()
    {
        await using var app = new ReservationsApiFactory();
        await app.SeedAsync();
        var client = app.CreateClient();
        await app.AuthorizeAsync(client, "user@example.com");

        var reservationResponse = await client.PostAsJsonAsync("/api/v1/reservations", new
        {
            sessionId = app.SessionId,
            seatIds = new[] { app.FirstSeatId }
        });
        Assert.Equal(HttpStatusCode.Created, reservationResponse.StatusCode);

        var seatMapResponse = await client.GetAsync($"/api/v1/sessions/{app.SessionId}/seat-map");

        Assert.Equal(HttpStatusCode.OK, seatMapResponse.StatusCode);
        var seatMap = await seatMapResponse.Content.ReadFromJsonAsync<SeatMapPayload>();
        Assert.NotNull(seatMap);
        Assert.Contains(seatMap.Seats, seat => seat.SeatId == app.FirstSeatId && seat.Status == "Reserved");
    }

    private sealed record ReservationPayload(
        Guid Id,
        Guid UserId,
        Guid SessionId,
        DateTime ExpiresAtUtc,
        string Status,
        List<Guid> SeatIds);

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

    private sealed class ReservationsApiFactory : WebApplicationFactory<Program>
    {
        private readonly string _databaseName = $"ticketflow-reservations-tests-{Guid.NewGuid()}";
        private readonly InMemorySeatLockStore _lockStore;

        public ReservationsApiFactory()
            : this(new InMemorySeatLockStore())
        {
        }

        public ReservationsApiFactory(InMemorySeatLockStore lockStore)
        {
            _lockStore = lockStore;
        }

        public Guid UserId { get; } = Guid.NewGuid();

        public Guid SecondUserId { get; } = Guid.NewGuid();

        public Guid OrganizerId { get; } = Guid.NewGuid();

        public Guid HallId { get; } = Guid.NewGuid();

        public Guid EventId { get; } = Guid.NewGuid();

        public Guid SessionId { get; } = Guid.NewGuid();

        public Guid FirstSeatId { get; private set; }

        public Guid SecondSeatId { get; private set; }

        public async Task SeedAsync()
        {
            using var scope = Services.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<TicketFlowDbContext>();
            await dbContext.Database.EnsureDeletedAsync();
            await dbContext.Database.EnsureCreatedAsync();

            dbContext.Users.AddRange(
                new User(UserId, "user@example.com", HashPassword("Password123!"), UserRole.User),
                new User(SecondUserId, "second-user@example.com", HashPassword("Password123!"), UserRole.User),
                new User(OrganizerId, "organizer@example.com", HashPassword("Password123!"), UserRole.Organizer));

            var hall = new Hall(HallId, "Main hall", 1, 2);
            var seats = hall.Seats.OrderBy(seat => seat.Row).ThenBy(seat => seat.Number).ToArray();
            FirstSeatId = seats[0].Id;
            SecondSeatId = seats[1].Id;
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
            await AuthorizeAsync(client, "user@example.com");
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

        public async Task<int> CountActiveReservationsAsync(Guid seatId)
        {
            using var scope = Services.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<TicketFlowDbContext>();

            return await dbContext.Reservations
                .CountAsync(reservation =>
                    reservation.Status == ReservationStatus.Active &&
                    reservation.Seats.Any(seat => seat.SeatId == seatId));
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
                services.RemoveAll<ISeatStatusNotifier>();
                services.AddDbContext<TicketFlowDbContext>(options =>
                {
                    options.UseInMemoryDatabase(_databaseName);
                });
                services.AddSingleton<ISeatLockStore>(_lockStore);
                services.AddSingleton<ISeatStatusNotifier, NoOpSeatStatusNotifier>();
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

    private sealed class NoOpSeatStatusNotifier : ISeatStatusNotifier
    {
        public Task SeatsReservedAsync(
            Guid sessionId,
            Guid reservationId,
            IReadOnlyCollection<Guid> seatIds,
            CancellationToken cancellationToken = default)
        {
            return Task.CompletedTask;
        }
    }

    private sealed class InMemorySeatLockStore : ISeatLockStore
    {
        private readonly ConcurrentDictionary<string, LockEntry> _locks = new(StringComparer.Ordinal);
        private readonly object _gate = new();

        public Task<bool> TryLockSeatsAsync(
            Guid sessionId,
            Guid reservationId,
            IReadOnlyCollection<Guid> seatIds,
            TimeSpan ttl,
            CancellationToken cancellationToken = default)
        {
            var expiresAtUtc = DateTime.UtcNow.Add(ttl);
            var keys = seatIds.Select(seatId => GetKey(sessionId, seatId)).ToArray();

            lock (_gate)
            {
                foreach (var key in keys)
                {
                    if (_locks.TryGetValue(key, out var existingLock) && existingLock.ExpiresAtUtc > DateTime.UtcNow)
                    {
                        return Task.FromResult(false);
                    }
                }

                foreach (var key in keys)
                {
                    _locks[key] = new LockEntry(reservationId, expiresAtUtc);
                }
            }

            return Task.FromResult(true);
        }

        public Task ReleaseSeatsAsync(
            Guid sessionId,
            Guid reservationId,
            IReadOnlyCollection<Guid> seatIds,
            CancellationToken cancellationToken = default)
        {
            lock (_gate)
            {
                foreach (var seatId in seatIds)
                {
                    var key = GetKey(sessionId, seatId);
                    if (_locks.TryGetValue(key, out var existingLock) && existingLock.ReservationId == reservationId)
                    {
                        _locks.TryRemove(key, out _);
                    }
                }
            }

            return Task.CompletedTask;
        }

        private static string GetKey(Guid sessionId, Guid seatId)
        {
            return $"{sessionId}:{seatId}";
        }

        private sealed record LockEntry(Guid ReservationId, DateTime ExpiresAtUtc);
    }
}
