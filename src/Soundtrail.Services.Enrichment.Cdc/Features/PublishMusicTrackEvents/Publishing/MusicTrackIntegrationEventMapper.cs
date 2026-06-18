using Soundtrail.Contracts.Common;
using Soundtrail.Contracts.EventSourcing;
using Soundtrail.Contracts.IntegrationMessaging.Commands;
using Soundtrail.Contracts.IntegrationMessaging.Events;
using System.Text.Json;

namespace Soundtrail.Services.Enrichment.Cdc.Features.PublishMusicTrackEvents.Publishing;

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
        var data = JsonSerializer.Deserialize<PlaybackReferencesResolutionRequiredEventDataRecordDto>(storedEvent.Data)
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
                data.Album),
            data.ArtistId,
            data.AlbumId);
    }
}
