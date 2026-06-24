using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Soundtrail.Services.Enrichment.Scheduler.Infrastructure.Hosting;

public class SchedulerHostedService(
    IServiceScopeFactory scopeFactory,
    IOptions<SchedulerOptions> options,
    ILogger<SchedulerHostedService> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var interval = TimeSpan.FromSeconds(options.Value.RunIntervalSeconds);
        while (!stoppingToken.IsCancellationRequested)
        {
            await ExecuteOneIteration(stoppingToken);

            await Task.Delay(interval, stoppingToken);
        }
    }

    protected internal virtual async Task ExecuteOneIteration(CancellationToken cancellationToken)
    {
        try
        {
            await using var scope = scopeFactory.CreateAsyncScope();
            var handlers = scope.ServiceProvider.GetServices<ISchedulerHandler>();
            foreach (var handler in handlers)
            {
                await handler.Handle(cancellationToken);
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Discovery backlog scheduling run failed.");
        }
    }
}
