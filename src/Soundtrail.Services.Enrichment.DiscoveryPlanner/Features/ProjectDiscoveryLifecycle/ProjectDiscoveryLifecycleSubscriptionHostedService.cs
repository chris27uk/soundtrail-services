using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Raven.Client.Documents;
using Raven.Client.Documents.Subscriptions;
using Soundtrail.Contracts.EventSourcing;
using Soundtrail.Services.Enrichment.DiscoveryPlanner.Features.ProjectDiscoveryLifecycle.Adapters;

namespace Soundtrail.Services.Enrichment.DiscoveryPlanner.Features.ProjectDiscoveryLifecycle;

public sealed class ProjectDiscoveryLifecycleSubscriptionHostedService(
    IDocumentStore documentStore,
    DiscoveryLifecycleProjectionApplier projectionApplier,
    ILogger<ProjectDiscoveryLifecycleSubscriptionHostedService> logger) : BackgroundService
{
    private const string SubscriptionName = "discovery-lifecycle-projections";

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await EnsureSubscriptionExistsAsync(stoppingToken);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var options = new SubscriptionWorkerOptions(SubscriptionName);
                await using var worker = documentStore.Subscriptions.GetSubscriptionWorker<DiscoveryQueryStoredEventRecordDto>(options);
                await worker.Run(batch => ProcessBatchAsync(batch, stoppingToken), stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (ObjectDisposedException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Discovery lifecycle projection subscription failed.");
                await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
            }
        }
    }

    private async Task EnsureSubscriptionExistsAsync(CancellationToken cancellationToken)
    {
        try
        {
            await documentStore.Subscriptions.CreateAsync<DiscoveryQueryStoredEventRecordDto>(
                new SubscriptionCreationOptions
                {
                    Name = SubscriptionName
                },
                token: cancellationToken);
        }
        catch (Exception)
        {
            // ignored
        }
    }

    private async Task ProcessBatchAsync(
        SubscriptionBatch<DiscoveryQueryStoredEventRecordDto> batch,
        CancellationToken cancellationToken)
    {
        using var session = documentStore.OpenAsyncSession();

        foreach (var storedEvent in batch.Items
                     .Select(item => item.Result)
                     .OrderBy(item => item.Criteria, StringComparer.Ordinal)
                     .ThenBy(item => item.Version))
        {
            await projectionApplier.ApplyStoredEventAsync(storedEvent, session, cancellationToken);
        }

        await session.SaveChangesAsync(cancellationToken);
    }
}
