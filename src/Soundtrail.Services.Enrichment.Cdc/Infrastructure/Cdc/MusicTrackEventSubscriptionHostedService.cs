using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Raven.Client.Documents;
using Raven.Client.Documents.Session;
using Raven.Client.Documents.Subscriptions;
using Soundtrail.Contracts;
using Soundtrail.Services.Enrichment.DiscoveryPlanner.Shared.Execution;
using Soundtrail.Services.Enrichment.DiscoveryPlanner.Shared.MusicTracks;
using Soundtrail.Services.Enrichment.DiscoveryPlanner.Shared.Prioritisation;
using Soundtrail.Services.Enrichment.DiscoveryPlanner.Shared.Search;
using Wolverine;

namespace Soundtrail.Services.Enrichment.Cdc.Infrastructure.Cdc;

public sealed class MusicTrackEventSubscriptionHostedService(
    IDocumentStore documentStore,
    IServiceScopeFactory serviceScopeFactory,
    ILogger<MusicTrackEventSubscriptionHostedService> logger) : BackgroundService
{
    private const string SubscriptionName = "music-track-events-cdc";

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await EnsureSubscriptionExistsAsync(stoppingToken);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var options = new SubscriptionWorkerOptions(SubscriptionName);
                await using var worker = documentStore.Subscriptions.GetSubscriptionWorker<RavenMusicTrackStreamDocument>(options);
                await worker.Run(batch => ProcessBatchAsync(batch, stoppingToken), stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "MusicTrack event subscription failed.");
                await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
            }
        }
    }

    private async Task EnsureSubscriptionExistsAsync(CancellationToken cancellationToken)
    {
        try
        {
            await documentStore.Subscriptions.CreateAsync<RavenMusicTrackStreamDocument>(
                new SubscriptionCreationOptions
                {
                    Name = SubscriptionName
                },
                token: cancellationToken);
        }
        catch (Exception)
        {
        }
    }

    private async Task ProcessBatchAsync(
        SubscriptionBatch<RavenMusicTrackStreamDocument> batch,
        CancellationToken cancellationToken)
    {
        using var scope = serviceScopeFactory.CreateScope();
        var messageBus = scope.ServiceProvider.GetRequiredService<IMessageBus>();
        using var session = documentStore.OpenAsyncSession();

        foreach (var item in batch.Items)
        {
            var stream = item.Result;
            var cursorId = RavenMusicTrackEventSubscriptionCursorDocument.GetDocumentId(stream.Id);
            var cursor = await session.LoadAsync<RavenMusicTrackEventSubscriptionCursorDocument>(cursorId, cancellationToken)
                ?? new RavenMusicTrackEventSubscriptionCursorDocument
                {
                    Id = cursorId,
                    StreamId = stream.Id
                };

            if (stream.Version <= cursor.LastPublishedVersion)
            {
                continue;
            }

            var newEvents = stream.Facts.Skip(cursor.LastPublishedVersion).ToArray();
            foreach (var @event in newEvents)
            {
                var domainEvent = ToDomainEvent(@event);
                if (domainEvent is not null)
                {
                    await messageBus.SendAsync(domainEvent);
                }
            }

            cursor.LastPublishedVersion = stream.Version;
            await session.StoreAsync(cursor, cancellationToken);
        }

        await session.SaveChangesAsync(cancellationToken);
    }

    private static MusicTrackFact? ToDomainEvent(RavenMusicTrackEventDocument @event)
    {
        return @event.Type switch
        {
            nameof(AppleMusicResolutionRequired) => new AppleMusicResolutionRequired(
                MusicCatalogId.From(@event.MusicCatalogId ?? string.Empty),
                Enum.Parse<LookupPriorityBand>(@event.Priority ?? nameof(LookupPriorityBand.Low), ignoreCase: true),
                CorrelationId.From(@event.CorrelationId ?? string.Empty),
                ProviderName.From(@event.SourceProvider),
                @event.ObservedAt),
            nameof(YouTubeMusicResolutionRequired) => new YouTubeMusicResolutionRequired(
                MusicCatalogId.From(@event.MusicCatalogId ?? string.Empty),
                Enum.Parse<LookupPriorityBand>(@event.Priority ?? nameof(LookupPriorityBand.Low), ignoreCase: true),
                CorrelationId.From(@event.CorrelationId ?? string.Empty),
                ProviderName.From(@event.SourceProvider),
                @event.ObservedAt),
            _ => null
        };
    }
}
