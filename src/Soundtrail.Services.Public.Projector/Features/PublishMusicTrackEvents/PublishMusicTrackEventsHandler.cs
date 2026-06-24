using Soundtrail.Contracts.EventSourcing;
using Soundtrail.Services.Public.Projector.Features.PublishMusicTrackEvents.Publishing;

namespace Soundtrail.Services.Public.Projector.Features.PublishMusicTrackEvents;

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
