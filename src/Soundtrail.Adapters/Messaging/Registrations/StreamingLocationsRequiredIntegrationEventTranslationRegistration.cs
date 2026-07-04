using Soundtrail.Contracts.IntegrationMessaging.Commands;
using Soundtrail.Contracts.IntegrationMessaging.Events;
using Soundtrail.Contracts.Common;
using Soundtrail.Domain.Search;
using Soundtrail.Domain.Catalog.IntegrationEvents;
using Soundtrail.Adapters.Registry;

namespace Soundtrail.Adapters.Messaging.Registrations;

public sealed class StreamingLocationsRequiredIntegrationEventTranslationRegistration : ITypeTranslationRegistration
{
    public void Register(TypeTranslationRegistry registry)
    {
        registry.RegisterPair<StreamingLocationsRequiredIntegrationEvent, StreamingLocationsRequiredMessageDto>(
            playback =>
                new StreamingLocationsRequiredMessageDto(
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
            dto =>
                new StreamingLocationsRequiredIntegrationEvent(
                    MusicCatalogId.From(dto.MusicCatalogId),
                    dto.Priority,
                    CorrelationId.From(dto.CorrelationId),
                    LookupSource.From(dto.SourceProvider),
                    dto.ObservedAt,
                    dto.SearchTerm.Kind switch
                    {
                        MusicSearchKind.UnifiedSearch => LookupCriteria.Query(
                            dto.SearchTerm.Query ?? throw new InvalidOperationException("Streaming locations lookup requires a query for unified search.")),
                        MusicSearchKind.Isrc => LookupCriteria.ExactIsrc(
                            dto.SearchTerm.Isrc ?? throw new InvalidOperationException("Streaming locations lookup requires an ISRC for ISRC lookups.")),
                        MusicSearchKind.TrackArtistAlbum => LookupCriteria.ByTrackArtistAlbum(
                            dto.SearchTerm.Title ?? throw new InvalidOperationException("Streaming locations lookup requires a title for track/artist/album lookups."),
                            dto.SearchTerm.Artist ?? throw new InvalidOperationException("Streaming locations lookup requires an artist for track/artist/album lookups."),
                            dto.SearchTerm.Album),
                        _ => throw new InvalidOperationException($"Unsupported music search kind '{dto.SearchTerm.Kind}'.")
                    },
                    dto.ArtistId,
                    dto.AlbumId));
    }
}
