using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Raven.Client.Documents;
using Raven.Client.Documents.Subscriptions;
using Soundtrail.Adapters.Registry;
using Soundtrail.Contracts.Common;
using Soundtrail.Contracts.EventSourcing;
using Soundtrail.Domain.Abstractions.EventSourcing;

namespace Soundtrail.Services.Enrichment.Orchestrator.Features.OnCatalogDiscoveryWorkChanged.Adapters;

public sealed class CatalogDiscoveryWorkChangedSubscriptionHostedService(
    IDocumentStore documentStore,
    IServiceScopeFactory scopeFactory,
    ILogger<CatalogDiscoveryWorkChangedSubscriptionHostedService> logger) : BackgroundService
{
    private const string SubscriptionName = "catalog-discovery-work-summary";

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await EnsureSubscriptionExistsAsync(stoppingToken);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var options = new SubscriptionWorkerOptions(SubscriptionName);
                await using var worker = documentStore.Subscriptions.GetSubscriptionWorker<RavenStoredEventRecord>(options);
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
                logger.LogError(ex, "Catalog discovery work summary subscription failed.");
                await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
            }
        }
    }

    private async Task EnsureSubscriptionExistsAsync(CancellationToken cancellationToken)
    {
        try
        {
            await documentStore.Subscriptions.CreateAsync<RavenStoredEventRecord>(
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
        SubscriptionBatch<RavenStoredEventRecord> batch,
        CancellationToken cancellationToken)
    {
        await using var scope = scopeFactory.CreateAsyncScope();
        var projector = scope.ServiceProvider.GetRequiredService<CatalogDiscoveryWorkSummaryProjector>();

        foreach (var stream in batch.Items
                     .Select(item => item.Result)
                     .GroupBy(item => item.StreamId, StringComparer.Ordinal))
        {
            await projector.ProjectAsync(
                MusicCatalogId.From(stream.Key),
                stream.OrderBy(item => item.Version)
                    .Select(item => (item.Version, Event: (IDomainEvent)TypeTranslationRegistry.Default.ToDomainObject(item.Body)))
                    .ToArray(),
                cancellationToken);
        }
    }
}
