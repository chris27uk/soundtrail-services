using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Wolverine;

namespace Soundtrail.Services.Enrichment.DiscoveryPlanner.Infrastructure.Scheduling;

public sealed class LookupPlanningSweepHostedService(
    IMessageBus messageBus,
    IOptions<LookupPlanningOptions> options,
    ILogger<LookupPlanningSweepHostedService> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var interval = TimeSpan.FromSeconds(options.Value.SweepIntervalSeconds);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await messageBus.InvokeAsync(
                    new RunDiscoveryBacklogScheduling(
                        DateTimeOffset.UtcNow,
                        options.Value.SweepBatchSize),
                    stoppingToken);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Lookup planning sweep failed.");
            }

            await Task.Delay(interval, stoppingToken);
        }
    }
}
