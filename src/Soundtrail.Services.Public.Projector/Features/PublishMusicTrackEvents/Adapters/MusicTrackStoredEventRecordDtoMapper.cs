using Soundtrail.Contracts.Common;
using Soundtrail.Contracts.EventSourcing;
using Soundtrail.Domain.Catalog.IntegrationEvents;
using Soundtrail.Domain.Commands;
using Soundtrail.Domain.Model;

namespace Soundtrail.Services.Public.Projector.Features.PublishMusicTrackEvents.Adapters;

internal static class MusicTrackStoredEventRecordDtoMapper
{
    private const string StreamingLocationsRequiredEventType = "StreamingLocationsRequired";

    public static PublishMusicTrackEventsCommand ToCommand(
        this IReadOnlyCollection<MusicTrackStoredEventRecordDto> storedEvents)
    {
        var events = storedEvents
            .Select(ToIntegrationEvent)
            .Where(x => x is not null)
            .Cast<VersionedMusicTrackIntegrationEvent>()
            .ToArray();

        return new PublishMusicTrackEventsCommand(events);
    }

    private static VersionedMusicTrackIntegrationEvent? ToIntegrationEvent(MusicTrackStoredEventRecordDto storedEvent) =>
        storedEvent.EventType switch
        {
            StreamingLocationsRequiredEventType => ToStreamingLocationsRequired(storedEvent),
            _ => null
        };

    private static VersionedMusicTrackIntegrationEvent ToStreamingLocationsRequired(
        MusicTrackStoredEventRecordDto storedEvent)
    {
        var data = storedEvent.StreamingLocationsRequired
            ?? throw new InvalidOperationException("Missing streaming locations required event data.");
        var musicCatalogId = MusicCatalogId.From(data.MusicCatalogId);

        return new VersionedMusicTrackIntegrationEvent(
            musicCatalogId,
            storedEvent.Version,
            new StreamingLocationsRequiredIntegrationEvent(
                musicCatalogId,
                Enum.Parse<LookupPriorityBand>(data.Priority, ignoreCase: true),
                CorrelationId.From(data.CorrelationId),
                ProviderName.From(data.SourceProvider),
                data.ObservedAt,
                !string.IsNullOrWhiteSpace(data.Isrc)
                    ? MusicSearchTerm.ByIsrc(data.Isrc)
                    : MusicSearchTerm.ByTrackArtistAlbum(
                        data.Title ?? throw new InvalidOperationException("Streaming locations required event is missing title."),
                        data.Artist ?? throw new InvalidOperationException("Streaming locations required event is missing artist."),
                        data.Album),
                data.ArtistId,
                data.AlbumId));
    }
}
