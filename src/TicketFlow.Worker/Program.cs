using TicketFlow.Worker;
using TicketFlow.Application;
using TicketFlow.Infrastructure;

var builder = Host.CreateApplicationBuilder(args);
builder.Services.AddOptions<ExpiredReservationWorkerOptions>()
    .Bind(builder.Configuration.GetSection(ExpiredReservationWorkerOptions.SectionName))
    .Validate(options => options.IntervalSeconds > 0, "Expired reservation worker interval must be positive.")
    .ValidateOnStart();
builder.Services.AddApplication(builder.Configuration);
builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddHostedService<Worker>();

var host = builder.Build();
host.Run();
