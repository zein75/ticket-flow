using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace TicketFlow.Infrastructure.Persistence;

public sealed class TicketFlowDbContextFactory : IDesignTimeDbContextFactory<TicketFlowDbContext>
{
    public TicketFlowDbContext CreateDbContext(string[] args)
    {
        var connectionString = Environment.GetEnvironmentVariable("ConnectionStrings__Postgres")
            ?? "Host=localhost;Port=5432;Database=ticketflow;Username=ticketflow;Password=ticketflow_password";

        var optionsBuilder = new DbContextOptionsBuilder<TicketFlowDbContext>();
        optionsBuilder.UseNpgsql(connectionString);

        return new TicketFlowDbContext(optionsBuilder.Options);
    }
}
