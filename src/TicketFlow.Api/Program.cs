using System.Security.Claims;
using System.Text;
using FluentValidation;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using TicketFlow.Api.Auth;
using TicketFlow.Api.Hubs;
using TicketFlow.Api.Middleware;
using TicketFlow.Api.Realtime;
using TicketFlow.Api.Routing;
using TicketFlow.Api.Validation;
using TicketFlow.Application;
using TicketFlow.Application.Abstractions.Realtime;
using TicketFlow.Infrastructure;
using TicketFlow.Infrastructure.Auth;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddControllers(options =>
{
    options.Filters.Add<FluentValidationActionFilter>();
});
builder.Services.Configure<ApiBehaviorOptions>(options =>
{
    options.InvalidModelStateResponseFactory = context =>
    {
        var errors = context.ModelState
            .Where(entry => entry.Value?.Errors.Count > 0)
            .ToDictionary(
                entry => entry.Key,
                entry => entry.Value!.Errors.Select(error => error.ErrorMessage).ToArray(),
                StringComparer.Ordinal);

        return new BadRequestObjectResult(
            FluentValidationActionFilter.CreateValidationProblemDetails(context, errors));
    };
});
builder.Services.AddValidatorsFromAssemblyContaining<RegisterRequestValidator>();
builder.Services.AddOpenApi();
builder.Services.AddSignalR();
builder.Services.AddApplication(builder.Configuration);
builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddScoped<ISeatStatusNotifier, SignalRSeatStatusNotifier>();
if (!builder.Environment.IsEnvironment("Testing"))
{
    builder.Services.AddHostedService<RedisSeatStatusSubscriber>();
}

var jwtOptions = builder.Configuration.GetSection(JwtOptions.SectionName).Get<JwtOptions>()
    ?? throw new InvalidOperationException("JWT options are not configured.");
new JwtOptionsValidator().Validate(null, jwtOptions).ThrowIfFailed();

builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = jwtOptions.Issuer,
            ValidateAudience = true,
            ValidAudience = jwtOptions.Audience,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtOptions.SigningKey)),
            RoleClaimType = ClaimTypes.Role,
            NameClaimType = ClaimTypes.NameIdentifier,
            ClockSkew = TimeSpan.FromMinutes(1)
        };
    });

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy(AuthorizationPolicies.AdminOnly, policy => policy.RequireRole("Admin"));
    options.AddPolicy(AuthorizationPolicies.OrganizerOnly, policy => policy.RequireRole("Organizer"));
    options.AddPolicy(AuthorizationPolicies.OrganizerOrAdmin, policy => policy.RequireRole("Organizer", "Admin"));
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseMiddleware<GlobalExceptionMiddleware>();
app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();
app.MapHub<SeatHub>(ApiRoutes.V1 + "/hubs/seats");

app.Run();

public partial class Program;
