namespace TicketFlow.Worker;

using Microsoft.Extensions.Options;
using TicketFlow.Application.Features.ReservationExpirations;

public class Worker : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<Worker> _logger;
    private readonly ExpiredReservationWorkerOptions _options;

    public Worker(
        IServiceScopeFactory scopeFactory,
        IOptions<ExpiredReservationWorkerOptions> options,
        ILogger<Worker> logger)
    {
        _scopeFactory = scopeFactory;
        _options = options.Value;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = _scopeFactory.CreateScope();
                var expirationService = scope.ServiceProvider.GetRequiredService<IExpiredReservationService>();
                var result = await expirationService.ExpireAsync(stoppingToken);

                if (result.Count > 0)
                {
                    _logger.LogInformation("Expired {ReservationCount} reservations.", result.Count);
                }
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception exception)
            {
                _logger.LogError(exception, "Failed to expire outdated reservations.");
            }

            await Task.Delay(GetInterval(), stoppingToken);
        }
    }

    private TimeSpan GetInterval()
    {
        if (_options.IntervalSeconds <= 0)
        {
            throw new InvalidOperationException("Expired reservation worker interval must be positive.");
        }

        return TimeSpan.FromSeconds(_options.IntervalSeconds);
    }
}
