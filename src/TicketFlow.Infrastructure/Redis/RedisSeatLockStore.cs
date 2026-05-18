using StackExchange.Redis;
using TicketFlow.Application.Abstractions.Reservations;

namespace TicketFlow.Infrastructure.Redis;

internal sealed class RedisSeatLockStore : ISeatLockStore
{
    private const string LockValuePrefix = "reservation:";
    private readonly IConnectionMultiplexer _connection;

    public RedisSeatLockStore(IConnectionMultiplexer connection)
    {
        _connection = connection;
    }

    public async Task<bool> TryLockSeatsAsync(
        Guid sessionId,
        Guid reservationId,
        IReadOnlyCollection<Guid> seatIds,
        TimeSpan ttl,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(seatIds);

        if (seatIds.Count == 0)
        {
            return false;
        }

        cancellationToken.ThrowIfCancellationRequested();

        var database = _connection.GetDatabase();
        var keys = seatIds.Select(seatId => (RedisKey)GetSeatLockKey(sessionId, seatId)).ToArray();
        var values = new RedisValue[]
        {
            $"{LockValuePrefix}{reservationId}",
            (long)ttl.TotalMilliseconds
        };

        const string script = """
            for i = 1, #KEYS do
                if redis.call('exists', KEYS[i]) == 1 then
                    return 0
                end
            end

            for i = 1, #KEYS do
                redis.call('psetex', KEYS[i], ARGV[2], ARGV[1])
            end

            return 1
            """;

        var result = (int)await database.ScriptEvaluateAsync(script, keys, values);

        return result == 1;
    }

    public async Task ReleaseSeatsAsync(
        Guid sessionId,
        Guid reservationId,
        IReadOnlyCollection<Guid> seatIds,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(seatIds);
        cancellationToken.ThrowIfCancellationRequested();

        if (seatIds.Count == 0)
        {
            return;
        }

        var database = _connection.GetDatabase();
        var keys = seatIds.Select(seatId => (RedisKey)GetSeatLockKey(sessionId, seatId)).ToArray();
        var values = new RedisValue[] { $"{LockValuePrefix}{reservationId}" };

        const string script = """
            for i = 1, #KEYS do
                if redis.call('get', KEYS[i]) == ARGV[1] then
                    redis.call('del', KEYS[i])
                end
            end

            return 1
            """;

        await database.ScriptEvaluateAsync(script, keys, values);
    }

    private static string GetSeatLockKey(Guid sessionId, Guid seatId)
    {
        return $"seat-lock:{sessionId}:{seatId}";
    }
}
