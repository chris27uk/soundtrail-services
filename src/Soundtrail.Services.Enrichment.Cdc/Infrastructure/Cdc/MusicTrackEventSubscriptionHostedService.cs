using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Raven.Client.Documents;
using Raven.Client.Documents.Subscriptions;
using Soundtrail.Contracts.Common;
using Soundtrail.Contracts.Events;
using Soundtrail.Domain.Events;
using Wolverine;

namespace Soundtrail.Services.Enrichment.Cdc.Infrastructure.Cdc;

public sealed class MusicTrackEventSubscriptionHostedService(
    IDocumentStore documentStore,
    IServiceScopeFactory serviceScopeFactory,
    ILogger<MusicTrackEventSubscriptionHostedService> logger) : BackgroundService
{
    private const string SubscriptionName = "music-track-events-cdc";
    private const string AppleMusicResolutionRequiredEventType = "AppleMusicResolutionRequired";
    private const string YouTubeMusicResolutionRequiredEventType = "YouTubeMusicResolutionRequired";

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await EnsureSubscriptionExistsAsync(stoppingToken);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var options = new SubscriptionWorkerOptions(SubscriptionName);
                await using var worker = documentStore.Subscriptions.GetSubscriptionWorker<RavenMusicTrackStreamDto>(options);
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
            await documentStore.Subscriptions.CreateAsync<RavenMusicTrackStreamDto>(
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
        SubscriptionBatch<RavenMusicTrackStreamDto> batch,
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

            var newEvents = stream.Facts.Skip(cursor.LastPublishedVersion).ToArray();
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

    private static object? ToIntegrationEvent(RavenMusicTrackEventDto @event)
    {
        return @event.Type switch
            {
                AppleMusicResolutionRequiredEventType => AppleMusicResolutionRequiredDto(@event),
                YouTubeMusicResolutionRequiredEventType => YouTubeMusicResolutionRequiredDto(@event),
                _ => null
            };
    }

    private static YouTubeMusicResolutionRequiredMessageDto YouTubeMusicResolutionRequiredDto(RavenMusicTrackEventDto @event)
    {
        return new YouTubeMusicResolutionRequiredMessageDto(
            @event.MusicCatalogId ?? string.Empty,
            Enum.Parse<LookupPriorityBand>(@event.Priority ?? nameof(LookupPriorityBand.Low), ignoreCase: true),
            @event.CorrelationId ?? string.Empty,
            @event.SourceProvider,
            @event.ObservedAt);
    }

    private static AppleMusicResolutionRequiredMessageDto AppleMusicResolutionRequiredDto(RavenMusicTrackEventDto @event)
    {
        return new AppleMusicResolutionRequiredMessageDto(
            @event.MusicCatalogId ?? string.Empty,
            Enum.Parse<LookupPriorityBand>(@event.Priority ?? nameof(LookupPriorityBand.Low), ignoreCase: true),
            @event.CorrelationId ?? string.Empty,
            @event.SourceProvider,
            @event.ObservedAt);
    }
}
