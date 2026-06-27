using Soundtrail.Contracts.Common;
using Soundtrail.Contracts.EventSourcing;
using Soundtrail.Domain.Catalog.IntegrationEvents;
using Soundtrail.Domain.Commands;
using Soundtrail.Domain.Events;
using Soundtrail.Domain.Model;
using Soundtrail.Translators.MusicTrackEventStore;

namespace Soundtrail.Services.Public.Projector.Features.PublishMusicTrackEvents.Adapters;

internal static class MusicTrackStoredEventRecordDtoMapper
{
    public static PublishMusicTrackEventsCommand ToCommand(
        this IReadOnlyCollection<MusicTrackStoredEventRecordDto> storedEvents,
        IMusicTrackStoredEventRecordTranslator translator)
    {
        var events = storedEvents
            .Select(storedEvent => ToIntegrationEvent(storedEvent, translator))
            .Where(x => x is not null)
            .Cast<VersionedMusicTrackIntegrationEvent>()
            .ToArray();

        return new PublishMusicTrackEventsCommand(events);
    }

    private static VersionedMusicTrackIntegrationEvent? ToIntegrationEvent(
        MusicTrackStoredEventRecordDto storedEvent,
        IMusicTrackStoredEventRecordTranslator translator) =>
        translator.ToDomainObject(storedEvent) switch
        {
            StreamingLocationsRequired streamingLocationsRequired => ToStreamingLocationsRequired(storedEvent, streamingLocationsRequired),
            _ => null
        };

    private static VersionedMusicTrackIntegrationEvent ToStreamingLocationsRequired(
        MusicTrackStoredEventRecordDto storedEvent,
        StreamingLocationsRequired streamingLocationsRequired)
    {
        var musicCatalogId = streamingLocationsRequired.MusicCatalogId;
        return new VersionedMusicTrackIntegrationEvent(
            musicCatalogId,
            storedEvent.Version,
            new StreamingLocationsRequiredIntegrationEvent(
                musicCatalogId,
                streamingLocationsRequired.Priority,
                streamingLocationsRequired.CorrelationId,
                streamingLocationsRequired.SourceProvider,
                streamingLocationsRequired.ObservedAt,
                streamingLocationsRequired.SearchCriteria,
                streamingLocationsRequired.Hierarchy?.ArtistId?.Value,
                streamingLocationsRequired.Hierarchy?.AlbumId?.Value));
    }
}
