using Soundtrail.Contracts.EventSourcing;
using Soundtrail.Contracts.IntegrationMessaging.Events;
using Soundtrail.Services.Enrichment.Cdc.Features.PublishMusicTrackEvents.Publishing;

namespace Soundtrail.Services.Enrichment.Cdc.Features.PublishMusicTrackEvents;

public sealed class PublishMusicTrackEventsHandler(
    IPublishMusicTrackIntegrationEvents publishMusicTrackIntegrationEvents)
{
    public async Task HandleAsync(
        IReadOnlyCollection<MusicTrackStoredEventRecordDto> storedEvents,
        CancellationToken cancellationToken)
    {
        var integrationEvents = storedEvents
            .OrderBy(x => x.MusicCatalogId, StringComparer.Ordinal)
            .ThenBy(x => x.Version)
            .Select(MusicTrackIntegrationEventMapper.ToIntegrationEvent)
            .Where(x => x is not null)
            .Cast<object>()
            .ToArray();

        if (integrationEvents.Length == 0)
        {
            return;
        }

        await publishMusicTrackIntegrationEvents.PublishAsync(integrationEvents, cancellationToken);
    }
}
