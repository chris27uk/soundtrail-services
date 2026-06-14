using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Raven.Client.Documents;
using Raven.Client.Documents.Subscriptions;
using Soundtrail.Contracts.Common;
using Soundtrail.Contracts.IntegrationMessaging.Commands;
using Soundtrail.Contracts.IntegrationMessaging.Events;
using Soundtrail.Domain.Events;
using Wolverine;

namespace Soundtrail.Services.Enrichment.Cdc.Infrastructure.Cdc;

public sealed class MusicTrackEventSubscriptionHostedService(
    IDocumentStore documentStore,
    IServiceScopeFactory serviceScopeFactory,
    ILogger<MusicTrackEventSubscriptionHostedService> logger) : BackgroundService
{
    private const string SubscriptionName = "music-track-events-cdc";
    private const string PlaybackReferencesResolutionRequiredEventType = "PlaybackReferencesResolutionRequired";

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await EnsureSubscriptionExistsAsync(stoppingToken);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var options = new SubscriptionWorkerOptions(SubscriptionName);
                await using var worker = documentStore.Subscriptions.GetSubscriptionWorker<RavenMusicTrackStreamRecordDto>(options);
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
                logger.LogError(ex, "MusicTrack event subscription failed.");
                await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
            }
        }
    }

    private async Task EnsureSubscriptionExistsAsync(CancellationToken cancellationToken)
    {
        try
        {
            await documentStore.Subscriptions.CreateAsync<RavenMusicTrackStreamRecordDto>(
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
        SubscriptionBatch<RavenMusicTrackStreamRecordDto> batch,
        CancellationToken cancellationToken)
    {
        using var scope = serviceScopeFactory.CreateScope();
        var messageBus = scope.ServiceProvider.GetRequiredService<IMessageBus>();
        using var session = documentStore.OpenAsyncSession();

        foreach (var item in batch.Items)
        {
            var stream = item.Result;
            var cursorId = RavenMusicTrackEventSubscriptionCursorDto.GetDocumentId(stream.Id);
            var cursor = await session.LoadAsync<RavenMusicTrackEventSubscriptionCursorDto>(cursorId, cancellationToken)
                ?? new RavenMusicTrackEventSubscriptionCursorDto
                {
                    Id = cursorId,
                    StreamId = stream.Id
                };

            if (stream.Version <= cursor.LastPublishedVersion)
            {
                continue;
            }

            var newEvents = stream.Events.Skip(cursor.LastPublishedVersion).ToArray();
            foreach (var @event in newEvents)
            {
                var integrationEvent = ToIntegrationEvent(@event);
                if (integrationEvent is not null)
                {
                    await messageBus.SendAsync(integrationEvent);
                }
            }

            cursor.LastPublishedVersion = stream.Version;
            await session.StoreAsync(cursor, cancellationToken);
        }

        await session.SaveChangesAsync(cancellationToken);
    }

    private static object? ToIntegrationEvent(RavenMusicTrackEventRecordDto @event)
    {
        return @event.Type switch
            {
                PlaybackReferencesResolutionRequiredEventType => PlaybackReferencesResolutionRequiredDto(@event),
                _ => null
            };
    }

    private static PlaybackReferencesResolutionRequiredMessageDto PlaybackReferencesResolutionRequiredDto(RavenMusicTrackEventRecordDto @event)
    {
        return new PlaybackReferencesResolutionRequiredMessageDto(
            @event.MusicCatalogId ?? string.Empty,
            Enum.Parse<LookupPriorityBand>(@event.Priority ?? nameof(LookupPriorityBand.Low), ignoreCase: true),
            @event.CorrelationId ?? string.Empty,
            @event.SourceProvider,
            @event.ObservedAt,
            new PlaybackReferenceSearchTermDto(
                @event.Isrc,
                @event.Title,
                @event.Artist,
                @event.Album));
    }
}
