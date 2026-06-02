using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Soundtrail.Services.Enrichment.Features.Scheduling;

namespace Soundtrail.Services.Enrichment.Worker.Infrastructure.Planning;

public sealed class LookupPlanningSweepHostedService(
    LookupPlanningSweep sweep,
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
                await sweep.RunOnceAsync(
                    DateTimeOffset.UtcNow,
                    options.Value.SweepBatchSize,
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
