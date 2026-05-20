using Microsoft.Extensions.Diagnostics.HealthChecks;
using TicketFlow.Infrastructure.Persistence;

namespace TicketFlow.Api.HealthChecks;

public sealed class PostgresHealthCheck : IHealthCheck
{
    private readonly TicketFlowDbContext _dbContext;

    public PostgresHealthCheck(TicketFlowDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        var canConnect = await _dbContext.Database.CanConnectAsync(cancellationToken);

        return canConnect
            ? HealthCheckResult.Healthy("PostgreSQL is reachable.")
            : HealthCheckResult.Unhealthy("PostgreSQL is unreachable.");
    }
}
