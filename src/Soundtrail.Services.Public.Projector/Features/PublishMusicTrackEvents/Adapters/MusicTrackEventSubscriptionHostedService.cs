using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Raven.Client.Documents;
using Raven.Client.Documents.Subscriptions;
using Soundtrail.Adapters.Registry;
using Soundtrail.Contracts.EventSourcing;
using Soundtrail.Domain.Abstractions;
using Soundtrail.Domain.Catalog.Commands;
using Soundtrail.Domain.Catalog.Events;
using Soundtrail.Domain.Catalog.IntegrationEvents;
using Soundtrail.Domain.Abstractions.EventSourcing;

namespace Soundtrail.Services.Public.Projector.Features.PublishMusicTrackEvents.Adapters;

public sealed class MusicTrackEventSubscriptionHostedService(
    IDocumentStore documentStore,
    IServiceScopeFactory serviceScopeFactory,
    ILogger<MusicTrackEventSubscriptionHostedService> logger) : BackgroundService
{
    private const string SubscriptionName = "artist-catalog-events-public";

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
                logger.LogError(ex, "MusicTrack event subscription failed.");
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
        using var scope = serviceScopeFactory.CreateScope();
        var handler = scope.ServiceProvider.GetRequiredService<IHandler<PublishMusicTrackEventsCommand>>();
        var registry = scope.ServiceProvider.GetRequiredService<ITypeRegistry>();
        var events = batch.Items
            .Select(x => x.Result)
            .Where(storedEvent => string.Equals(storedEvent.AggregateType, "artist-catalog", StringComparison.Ordinal))
            .Select(storedEvent =>
            {
                if (storedEvent.Body is null)
                {
                    throw new InvalidOperationException($"Stored event '{storedEvent.Id}' is missing a body.");
                }

                return (Stored: storedEvent, Event: registry.ToDomainObject<IDomainEvent>(storedEvent.Body));
            })
            .Where(x => x.Event is StreamingLocationsRequired)
            .Select(x =>
            {
                var streamingLocationsRequired = (StreamingLocationsRequired)x.Event;
                return new VersionedMusicTrackIntegrationEvent(
                    streamingLocationsRequired.MusicCatalogId,
                    x.Stored.Version,
                    new StreamingLocationsRequiredIntegrationEvent(
                        streamingLocationsRequired.MusicCatalogId,
                        streamingLocationsRequired.Priority,
                        streamingLocationsRequired.CorrelationId,
                        streamingLocationsRequired.SourceProvider,
                        streamingLocationsRequired.ObservedAt,
                        streamingLocationsRequired.SearchCriteria,
                        streamingLocationsRequired.Hierarchy?.ArtistId?.Value,
                        streamingLocationsRequired.Hierarchy?.AlbumId?.Value));
            })
            .ToArray();

        await handler.Handle(new PublishMusicTrackEventsCommand(events), cancellationToken);
    }
}
