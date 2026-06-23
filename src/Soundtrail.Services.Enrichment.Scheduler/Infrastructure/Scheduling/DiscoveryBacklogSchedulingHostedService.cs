using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Soundtrail.Contracts.IntegrationMessaging.Commands;
using Wolverine;

namespace Soundtrail.Services.Enrichment.Scheduler.Infrastructure.Scheduling;

public sealed class DiscoveryBacklogSchedulingHostedService(
    IServiceScopeFactory scopeFactory,
    IOptions<DiscoveryBacklogSchedulingOptions> options,
    ILogger<DiscoveryBacklogSchedulingHostedService> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var interval = TimeSpan.FromSeconds(options.Value.RunIntervalSeconds);
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await using var scope = scopeFactory.CreateAsyncScope();
                var messageBus = scope.ServiceProvider.GetRequiredService<IMessageBus>();
                await messageBus.SendAsync(
                    new RunDiscoveryBacklogSchedulingCommandDto(
                        DateTimeOffset.UtcNow,
                        options.Value.BatchSize));
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Discovery backlog scheduling run failed.");
            }

            await Task.Delay(interval, stoppingToken);
        }
    }
}
