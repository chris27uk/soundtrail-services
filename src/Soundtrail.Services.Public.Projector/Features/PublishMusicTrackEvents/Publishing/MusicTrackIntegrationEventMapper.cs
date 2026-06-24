using Soundtrail.Contracts.Common;
using Soundtrail.Contracts.EventSourcing;
using Soundtrail.Contracts.IntegrationMessaging.Commands;
using Soundtrail.Contracts.IntegrationMessaging.Events;

namespace Soundtrail.Services.Public.Projector.Features.PublishMusicTrackEvents.Publishing;

internal static class MusicTrackIntegrationEventMapper
{
    private const string PlaybackReferencesResolutionRequiredEventType = "PlaybackReferencesResolutionRequired";

    public static object? ToIntegrationEvent(MusicTrackStoredEventRecordDto storedEvent)
    {
        return storedEvent.EventType switch
        {
            PlaybackReferencesResolutionRequiredEventType => ToPlaybackReferencesResolutionRequired(storedEvent),
            _ => null
        };
    }

    private static PlaybackReferencesResolutionRequiredMessageDto ToPlaybackReferencesResolutionRequired(
        MusicTrackStoredEventRecordDto storedEvent)
    {
        var data = storedEvent.PlaybackReferencesResolutionRequired
            ?? throw new InvalidOperationException("Missing playback references resolution required event data.");

        return new PlaybackReferencesResolutionRequiredMessageDto(
            data.MusicCatalogId,
            Enum.Parse<LookupPriorityBand>(data.Priority, ignoreCase: true),
            data.CorrelationId,
            data.SourceProvider,
            data.ObservedAt,
            new StreamingLocationSearchTermDto(
                data.Isrc,
                data.Title,
                data.Artist,
                data.Album),
            data.ArtistId,
            data.AlbumId);
    }
}
