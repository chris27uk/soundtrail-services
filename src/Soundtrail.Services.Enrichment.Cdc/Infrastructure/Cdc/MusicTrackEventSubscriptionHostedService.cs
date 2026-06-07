using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Raven.Client.Documents;
using Raven.Client.Documents.Session;
using Raven.Client.Documents.Subscriptions;
using Soundtrail.Services.Enrichment.Shared.Execution;
using Soundtrail.Services.Enrichment.Shared.MusicTracks;
using Soundtrail.Services.Enrichment.Shared.Prioritisation;
using Soundtrail.Services.Enrichment.Shared.Search;
using Soundtrail.Services.Shared;
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
        var handler = scope.ServiceProvider.GetRequiredService<MusicTrackEventCommandHandler>();
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
                switch (ToDomainEvent(@event))
                {
                    case AppleMusicResolutionRequired appleMusicResolutionRequired:
                        await messageBus.SendAsync(handler.Handle(appleMusicResolutionRequired));
                        break;
                    case YouTubeMusicResolutionRequired youTubeMusicResolutionRequired:
                        await messageBus.SendAsync(handler.Handle(youTubeMusicResolutionRequired));
                        break;
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
                Enum.Parse<ProviderName>(@event.SourceProvider, ignoreCase: true),
                @event.ObservedAt),
            nameof(YouTubeMusicResolutionRequired) => new YouTubeMusicResolutionRequired(
                MusicCatalogId.From(@event.MusicCatalogId ?? string.Empty),
                Enum.Parse<LookupPriorityBand>(@event.Priority ?? nameof(LookupPriorityBand.Low), ignoreCase: true),
                CorrelationId.From(@event.CorrelationId ?? string.Empty),
                Enum.Parse<ProviderName>(@event.SourceProvider, ignoreCase: true),
                @event.ObservedAt),
            _ => null
        };
    }
}
