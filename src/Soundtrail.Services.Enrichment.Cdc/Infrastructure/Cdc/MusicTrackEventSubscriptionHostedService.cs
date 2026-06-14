using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Raven.Client.Documents;
using Raven.Client.Documents.Subscriptions;
using Soundtrail.Contracts.Common;
using Soundtrail.Contracts.EventSourcing;
using Soundtrail.Contracts.IntegrationMessaging.Commands;
using Soundtrail.Contracts.IntegrationMessaging.Events;
using System.Text.Json;
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
                logger.LogError(ex, "MusicTrack event subscription failed.");
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
        using var scope = serviceScopeFactory.CreateScope();
        var messageBus = scope.ServiceProvider.GetRequiredService<IMessageBus>();

        foreach (var item in batch.Items)
        {
            var integrationEvent = ToIntegrationEvent(item.Result);
            if (integrationEvent is not null)
            {
                await messageBus.SendAsync(integrationEvent);
            }
        }
    }

    private static object? ToIntegrationEvent(MusicTrackStoredEventRecordDto @event)
    {
        return @event.EventType switch
            {
                PlaybackReferencesResolutionRequiredEventType => PlaybackReferencesResolutionRequiredDto(@event),
                _ => null
            };
    }

    private static PlaybackReferencesResolutionRequiredMessageDto PlaybackReferencesResolutionRequiredDto(MusicTrackStoredEventRecordDto @event)
    {
        var data = JsonSerializer.Deserialize<PlaybackReferencesResolutionRequiredEventDataRecordDto>(@event.Data)
            ?? throw new InvalidOperationException("Unable to deserialize playback references resolution required event data.");

        return new PlaybackReferencesResolutionRequiredMessageDto(
            data.MusicCatalogId,
            Enum.Parse<LookupPriorityBand>(data.Priority, ignoreCase: true),
            data.CorrelationId,
            data.SourceProvider,
            data.ObservedAt,
            new PlaybackReferenceSearchTermDto(
                data.Isrc,
                data.Title,
                data.Artist,
                data.Album));
    }
}
