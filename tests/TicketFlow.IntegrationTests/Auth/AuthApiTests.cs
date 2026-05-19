using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using TicketFlow.Application.Features.Auth;
using TicketFlow.Infrastructure.Persistence;

namespace TicketFlow.IntegrationTests.Auth;

public sealed class AuthApiTests
{
    [Fact]
    public async Task RegisterLoginRefreshAndMe_ShouldCompleteAuthFlow()
    {
        await using var app = new AuthApiFactory();
        var client = app.CreateClient();

        var registerResponse = await client.PostAsJsonAsync("/api/v1/auth/register", new
        {
            email = "user@example.com",
            password = "Password123!"
        });

        Assert.Equal(HttpStatusCode.Created, registerResponse.StatusCode);

        var registerPayload = await registerResponse.Content.ReadFromJsonAsync<AuthPayload>();
        Assert.NotNull(registerPayload);
        Assert.Equal("User", registerPayload.Role);
        Assert.False(string.IsNullOrWhiteSpace(registerPayload.AccessToken));
        Assert.False(string.IsNullOrWhiteSpace(registerPayload.RefreshToken));

        var loginResponse = await client.PostAsJsonAsync("/api/v1/auth/login", new
        {
            email = "user@example.com",
            password = "Password123!"
        });

        Assert.Equal(HttpStatusCode.OK, loginResponse.StatusCode);

        var loginPayload = await loginResponse.Content.ReadFromJsonAsync<AuthPayload>();
        Assert.NotNull(loginPayload);

        var refreshResponse = await client.PostAsJsonAsync("/api/v1/auth/refresh", new
        {
            refreshToken = loginPayload.RefreshToken
        });

        Assert.Equal(HttpStatusCode.OK, refreshResponse.StatusCode);

        var refreshPayload = await refreshResponse.Content.ReadFromJsonAsync<AuthPayload>();
        Assert.NotNull(refreshPayload);

        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", refreshPayload.AccessToken);
        var meResponse = await client.GetAsync("/api/v1/auth/me");

        Assert.Equal(HttpStatusCode.OK, meResponse.StatusCode);
    }

    [Fact]
    public async Task Register_ShouldRejectWhitespacePassword()
    {
        await using var app = new AuthApiFactory();
        var client = app.CreateClient();

        var response = await client.PostAsJsonAsync("/api/v1/auth/register", new
        {
            email = "user@example.com",
            password = "        "
        });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        Assert.Equal("https://ticketflow/errors/validation", document.RootElement.GetProperty("type").GetString());
        Assert.Equal("Validation failed.", document.RootElement.GetProperty("title").GetString());
        Assert.True(document.RootElement.TryGetProperty("errors", out var errors));
        Assert.True(errors.TryGetProperty("Password", out _));
        Assert.True(document.RootElement.TryGetProperty("traceId", out _));
    }

    [Fact]
    public async Task Refresh_ShouldRejectWhitespaceToken()
    {
        await using var app = new AuthApiFactory();
        var client = app.CreateClient();

        var response = await client.PostAsJsonAsync("/api/v1/auth/refresh", new
        {
            refreshToken = "   "
        });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task ReusingRefreshToken_ShouldReturnUnauthorized()
    {
        await using var app = new AuthApiFactory();
        var client = app.CreateClient();

        var registerResponse = await client.PostAsJsonAsync("/api/v1/auth/register", new
        {
            email = "user@example.com",
            password = "Password123!"
        });
        var registerPayload = await registerResponse.Content.ReadFromJsonAsync<AuthPayload>();
        Assert.NotNull(registerPayload);

        var firstRefresh = await client.PostAsJsonAsync("/api/v1/auth/refresh", new
        {
            refreshToken = registerPayload.RefreshToken
        });
        Assert.Equal(HttpStatusCode.OK, firstRefresh.StatusCode);

        var secondRefresh = await client.PostAsJsonAsync("/api/v1/auth/refresh", new
        {
            refreshToken = registerPayload.RefreshToken
        });

        Assert.Equal(HttpStatusCode.Unauthorized, secondRefresh.StatusCode);
    }

    [Fact]
    public async Task UnhandledException_ShouldReturnProblemDetails()
    {
        await using var app = new ThrowingAuthApiFactory();
        var client = app.CreateClient();

        var response = await client.PostAsJsonAsync("/api/v1/auth/login", new
        {
            email = "user@example.com",
            password = "Password123!"
        });

        Assert.Equal(HttpStatusCode.InternalServerError, response.StatusCode);
        using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        Assert.Equal("https://ticketflow/errors/internal-server-error", document.RootElement.GetProperty("type").GetString());
        Assert.Equal("An unexpected error occurred.", document.RootElement.GetProperty("title").GetString());
        Assert.True(document.RootElement.TryGetProperty("traceId", out _));
    }

    private sealed record AuthPayload(
        Guid UserId,
        string Email,
        string Role,
        string AccessToken,
        string RefreshToken,
        DateTime AccessTokenExpiresAtUtc,
        DateTime RefreshTokenExpiresAtUtc);

    private class AuthApiFactory : WebApplicationFactory<Program>
    {
        private readonly string _databaseName = $"ticketflow-auth-tests-{Guid.NewGuid()}";

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
    }

    private sealed class ThrowingAuthApiFactory : AuthApiFactory
    {
        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            base.ConfigureWebHost(builder);

            builder.ConfigureServices(services =>
            {
                services.RemoveAll<IAuthService>();
                services.AddScoped<IAuthService, ThrowingAuthService>();
            });
        }
    }

    private sealed class ThrowingAuthService : IAuthService
    {
        public Task<AuthResult> RegisterAsync(RegisterCommand command, CancellationToken cancellationToken = default) =>
            throw new InvalidOperationException("Synthetic auth failure.");

        public Task<AuthResult> LoginAsync(LoginCommand command, CancellationToken cancellationToken = default) =>
            throw new InvalidOperationException("Synthetic auth failure.");

        public Task<AuthResult> RefreshAsync(RefreshTokenCommand command, CancellationToken cancellationToken = default) =>
            throw new InvalidOperationException("Synthetic auth failure.");

        public Task LogoutAsync(LogoutCommand command, CancellationToken cancellationToken = default) =>
            throw new InvalidOperationException("Synthetic auth failure.");
    }
}
