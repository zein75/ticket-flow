using System.Text.Json;
using StackExchange.Redis;
using TicketFlow.Application.Abstractions.Realtime;

namespace TicketFlow.Infrastructure.Redis;

internal sealed class RedisSeatStatusNotifier : ISeatStatusNotifier
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    private readonly IConnectionMultiplexer _redis;

    public RedisSeatStatusNotifier(IConnectionMultiplexer redis)
    {
        _redis = redis;
    }

    public Task SeatsReservedAsync(
        Guid sessionId,
        Guid reservationId,
        IReadOnlyCollection<Guid> seatIds,
        CancellationToken cancellationToken = default)
    {
        return PublishAsync(new SeatStatusChangedMessage(
            SeatStatusChangeType.Reserved,
            sessionId,
            reservationId,
            null,
            seatIds));
    }

    public Task SeatsReleasedAsync(
        Guid sessionId,
        Guid reservationId,
        IReadOnlyCollection<Guid> seatIds,
        CancellationToken cancellationToken = default)
    {
        return PublishAsync(new SeatStatusChangedMessage(
            SeatStatusChangeType.Released,
            sessionId,
            reservationId,
            null,
            seatIds));
    }

    public Task SeatsSoldAsync(
        Guid sessionId,
        Guid orderId,
        IReadOnlyCollection<Guid> seatIds,
        CancellationToken cancellationToken = default)
    {
        return PublishAsync(new SeatStatusChangedMessage(
            SeatStatusChangeType.Sold,
            sessionId,
            Guid.Empty,
            orderId,
            seatIds));
    }

    private async Task PublishAsync(SeatStatusChangedMessage message)
    {
        var subscriber = _redis.GetSubscriber();
        var payload = JsonSerializer.Serialize(message, JsonOptions);

        await subscriber.PublishAsync(RedisChannel.Literal(RedisSeatStatusChannels.SeatStatus), payload);
    }
}
