using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Raven.Client.Documents;
using Raven.Client.Documents.Subscriptions;
using Soundtrail.Contracts.Common;
using Soundtrail.Contracts.EventSourcing;
using Soundtrail.Domain.Catalog.Commands;
using Soundtrail.Domain.Catalog.Projection;
using Soundtrail.Adapters.MusicTrackEventStore;

namespace Soundtrail.Services.Internal.Projector.Features.OnMusicCatalogChanged.Adapters;

public sealed class MusicCatalogChangedSubscriptionHostedService(
    IDocumentStore documentStore,
    MusicCatalogChangedHandler handler,
    ILogger<MusicCatalogChangedSubscriptionHostedService> logger,
    IMusicTrackStoredEventRecordTranslator translator) : BackgroundService
{
    private const string SubscriptionName = "catalog-music-track-projections";

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await EnsureSubscriptionExistsAsync(stoppingToken);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var options = new SubscriptionWorkerOptions(SubscriptionName);
                await using var worker = documentStore.Subscriptions.GetSubscriptionWorker<MusicTrackStoredEventRecordDto>(options);
                await worker.Run(
                    async batch =>
                    {
                        foreach (var stream in batch.Items.Select(item => item.Result).GroupBy(x => x.MusicCatalogId, StringComparer.Ordinal))
                        {
                            await handler.Handle(
                                new MusicCatalogChangedCommand(
                                    MusicCatalogId.From(stream.Key),
                                    stream.OrderBy(x => x.Version)
                                        .Select(x => new VersionedMusicTrackEvent(x.Version, translator.ToDomainObject(x)))
                                        .ToArray()),
                                stoppingToken);
                        }
                    },
                    stoppingToken);
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
                logger.LogError(ex, "Catalog projection subscription failed.");
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
}
