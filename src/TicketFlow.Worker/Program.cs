using TicketFlow.Worker;
using TicketFlow.Application;
using TicketFlow.Infrastructure;

var builder = Host.CreateApplicationBuilder(args);
builder.Services.AddApplication(builder.Configuration);
builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddHostedService<Worker>();

var host = builder.Build();
host.Run();
