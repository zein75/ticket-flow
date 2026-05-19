using System.Text.Json;
using Microsoft.AspNetCore.SignalR;
using StackExchange.Redis;
using TicketFlow.Api.Hubs;
using TicketFlow.Application.Abstractions.Realtime;
using TicketFlow.Infrastructure.Redis;

namespace TicketFlow.Api.Realtime;

internal sealed class RedisSeatStatusSubscriber : BackgroundService
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    private readonly IConnectionMultiplexer _redis;
    private readonly IHubContext<SeatHub> _hubContext;
    private readonly ILogger<RedisSeatStatusSubscriber> _logger;

    public RedisSeatStatusSubscriber(
        IConnectionMultiplexer redis,
        IHubContext<SeatHub> hubContext,
        ILogger<RedisSeatStatusSubscriber> logger)
    {
        _redis = redis;
        _hubContext = hubContext;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var subscriber = _redis.GetSubscriber();
        Action<RedisChannel, RedisValue> handler = (channel, value) =>
        {
            _ = HandleAsync(value, stoppingToken);
        };
        await subscriber.SubscribeAsync(
            RedisChannel.Literal(RedisSeatStatusChannels.SeatStatus),
            handler);

        try
        {
            await Task.Delay(Timeout.InfiniteTimeSpan, stoppingToken);
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
            await subscriber.UnsubscribeAsync(RedisChannel.Literal(RedisSeatStatusChannels.SeatStatus));
        }
    }

    private async Task HandleAsync(RedisValue value, CancellationToken cancellationToken)
    {
        try
        {
            var message = JsonSerializer.Deserialize<SeatStatusChangedMessage>(value!, JsonOptions);
            if (message is null)
            {
                return;
            }

            var groupName = SeatHub.GetSessionGroupName(message.SessionId);
            switch (message.Type)
            {
                case SeatStatusChangeType.Reserved:
                    await _hubContext.Clients
                        .Group(groupName)
                        .SendAsync(
                            "SeatReserved",
                            new SeatReservedEvent(message.SessionId, message.ReservationId, message.SeatIds),
                            cancellationToken);
                    break;

                case SeatStatusChangeType.Released:
                    await _hubContext.Clients
                        .Group(groupName)
                        .SendAsync(
                            "SeatReleased",
                            new SeatReleasedEvent(message.SessionId, message.ReservationId, message.SeatIds),
                            cancellationToken);
                    break;

                case SeatStatusChangeType.Sold when message.OrderId is not null:
                    await _hubContext.Clients
                        .Group(groupName)
                        .SendAsync(
                            "SeatSold",
                            new SeatSoldEvent(message.SessionId, message.OrderId.Value, message.SeatIds),
                            cancellationToken);
                    break;

                default:
                    _logger.LogWarning("Unsupported Redis seat status message type {Type}.", message.Type);
                    break;
            }
        }
        catch (JsonException exception)
        {
            _logger.LogWarning(exception, "Could not deserialize Redis seat status message.");
        }
        catch (Exception exception) when (!cancellationToken.IsCancellationRequested)
        {
            _logger.LogError(exception, "Could not forward Redis seat status message to SignalR.");
        }
    }
}
