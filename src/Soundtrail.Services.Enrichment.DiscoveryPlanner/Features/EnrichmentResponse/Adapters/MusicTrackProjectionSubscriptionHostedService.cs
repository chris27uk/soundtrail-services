using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Raven.Client.Documents;
using Raven.Client.Documents.Subscriptions;
using Soundtrail.Contracts.EventSourcing;

namespace Soundtrail.Services.Enrichment.DiscoveryPlanner.Features.EnrichmentResponse.Adapters;

public sealed class MusicTrackProjectionSubscriptionHostedService(
    IDocumentStore documentStore,
    MusicTrackProjectionApplier projectionApplier,
    ILogger<MusicTrackProjectionSubscriptionHostedService> logger) : BackgroundService
{
    private const string SubscriptionName = "music-track-projections";

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await EnsureSubscriptionExistsAsync(stoppingToken);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var options = new SubscriptionWorkerOptions(SubscriptionName);
                await using var worker = documentStore.Subscriptions.GetSubscriptionWorker<MusicTrackStoredEventRecordDto>(options);
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
                logger.LogError(ex, "MusicTrack projection subscription failed.");
                await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
            }
        }
    }

    private async Task EnsureSubscriptionExistsAsync(CancellationToken cancellationToken)
    {
        try
        {
            await documentStore.Subscriptions.CreateAsync<MusicTrackStoredEventRecordDto>(
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
        SubscriptionBatch<MusicTrackStoredEventRecordDto> batch,
        CancellationToken cancellationToken)
    {
        using var session = documentStore.OpenAsyncSession();

        foreach (var storedEvent in batch.Items
                     .Select(item => item.Result)
                     .OrderBy(item => item.MusicCatalogId, StringComparer.Ordinal)
                     .ThenBy(item => item.Version))
        {
            await projectionApplier.ApplyStoredEventAsync(storedEvent, session, cancellationToken);
        }

        await session.SaveChangesAsync(cancellationToken);
    }
}
