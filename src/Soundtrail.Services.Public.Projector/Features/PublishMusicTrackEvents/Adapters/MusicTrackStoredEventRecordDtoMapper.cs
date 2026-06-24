using Soundtrail.Contracts.Common;
using Soundtrail.Contracts.EventSourcing;
using Soundtrail.Domain.Catalog.IntegrationEvents;
using Soundtrail.Domain.Commands;
using Soundtrail.Domain.Model;

namespace Soundtrail.Services.Public.Projector.Features.PublishMusicTrackEvents.Adapters;

internal static class MusicTrackStoredEventRecordDtoMapper
{
    private const string PlaybackReferencesResolutionRequiredEventType = "PlaybackReferencesResolutionRequired";

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
            PlaybackReferencesResolutionRequiredEventType => ToPlaybackReferencesResolutionRequired(storedEvent),
            _ => null
        };

    private static VersionedMusicTrackIntegrationEvent ToPlaybackReferencesResolutionRequired(
        MusicTrackStoredEventRecordDto storedEvent)
    {
        var data = storedEvent.StreamingLocationsRequired
            ?? throw new InvalidOperationException("Missing playback references resolution required event data.");
        var musicCatalogId = MusicCatalogId.From(data.MusicCatalogId);

        return new VersionedMusicTrackIntegrationEvent(
            musicCatalogId,
            storedEvent.Version,
            new PlaybackReferencesResolutionRequiredIntegrationEvent(
                musicCatalogId,
                Enum.Parse<LookupPriorityBand>(data.Priority, ignoreCase: true),
                CorrelationId.From(data.CorrelationId),
                ProviderName.From(data.SourceProvider),
                data.ObservedAt,
                !string.IsNullOrWhiteSpace(data.Isrc)
                    ? MusicSearchTerm.ByIsrc(data.Isrc)
                    : MusicSearchTerm.ByTrackArtistAlbum(
                        data.Title ?? throw new InvalidOperationException("Playback references resolution required event is missing title."),
                        data.Artist ?? throw new InvalidOperationException("Playback references resolution required event is missing artist."),
                        data.Album),
                data.ArtistId,
                data.AlbumId));
    }
}
