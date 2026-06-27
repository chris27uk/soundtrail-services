using Soundtrail.Contracts.IntegrationMessaging.Commands;
using Soundtrail.Contracts.IntegrationMessaging.Events;
using Soundtrail.Domain.Catalog.IntegrationEvents;

namespace Soundtrail.Adapters.IntegrationMessaging;

public static class MusicTrackIntegrationMessageTranslator
{
    public static object ToMessage(this MusicTrackIntegrationEvent integrationEvent) =>
        integrationEvent switch
        {
            StreamingLocationsRequiredIntegrationEvent playback => new StreamingLocationsRequiredMessageDto(
                playback.MusicCatalogId.Value,
                playback.Priority,
                playback.CorrelationId.Value,
                playback.SourceProvider.Value,
                playback.ObservedAt,
                new StreamingLocationSearchTermDto(
                    playback.SearchCriteria.Kind,
                    playback.SearchCriteria.UnifiedQuery,
                    playback.SearchCriteria.Isrc,
                    playback.SearchCriteria.Title,
                    playback.SearchCriteria.Artist,
                    playback.SearchCriteria.Album),
                playback.ArtistId,
                playback.AlbumId),
            _ => throw new ArgumentOutOfRangeException(nameof(integrationEvent), integrationEvent, null)
        };
}
