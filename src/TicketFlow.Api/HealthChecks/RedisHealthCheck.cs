using Microsoft.Extensions.Diagnostics.HealthChecks;
using StackExchange.Redis;

namespace TicketFlow.Api.HealthChecks;

public sealed class RedisHealthCheck : IHealthCheck
{
    private readonly IConnectionMultiplexer _redis;

    public RedisHealthCheck(IConnectionMultiplexer redis)
    {
        _redis = redis;
    }

    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        if (!_redis.IsConnected)
        {
            return HealthCheckResult.Unhealthy("Redis is disconnected.");
        }

        var database = _redis.GetDatabase();
        var latency = await database.PingAsync();

        return HealthCheckResult.Healthy("Redis is reachable.", new Dictionary<string, object>
        {
            ["latencyMs"] = latency.TotalMilliseconds
        });
    }
}
