using TicketFlow.Worker;
using TicketFlow.Application;
using TicketFlow.Infrastructure;
using Serilog;

var builder = Host.CreateApplicationBuilder(args);
builder.Services.AddSerilog((services, loggerConfiguration) =>
{
    loggerConfiguration
        .ReadFrom.Configuration(builder.Configuration)
        .ReadFrom.Services(services)
        .Enrich.FromLogContext()
        .Enrich.WithProperty("Application", "TicketFlow.Worker")
        .WriteTo.Console();
});
builder.Services.AddOptions<ExpiredReservationWorkerOptions>()
    .Bind(builder.Configuration.GetSection(ExpiredReservationWorkerOptions.SectionName))
    .Validate(options => options.IntervalSeconds > 0, "Expired reservation worker interval must be positive.")
    .ValidateOnStart();
builder.Services.AddApplication(builder.Configuration);
builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddHostedService<Worker>();

var host = builder.Build();
host.Run();
