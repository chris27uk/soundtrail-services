using Soundtrail.Contracts.Common;
using Soundtrail.Contracts.EventSourcing;
using Soundtrail.Domain.Catalog;
using Soundtrail.Domain.Catalog.Events;
using Soundtrail.Domain.Search;

namespace Soundtrail.Translators.MusicTrackEventStore.Registrations;

public sealed class StreamingLocationsRequiredStoredEventTranslationRegistration : IMusicTrackStoredEventTranslationRegistration
{
    public void Register(MusicTrackStoredEventTranslationRegistry registry)
    {
        registry.Register<StreamingLocationsRequired, StreamingLocationsRequiredEventDataRecordDto>(
            nameof(StreamingLocationsRequired),
            domainEvent => new StreamingLocationsRequiredEventDataRecordDto(
                domainEvent.MusicCatalogId.Value,
                domainEvent.Priority.ToString(),
                domainEvent.CorrelationId.Value,
                domainEvent.SourceProvider.Value,
                domainEvent.ObservedAt,
                domainEvent.SearchCriteria.Kind,
                domainEvent.SearchCriteria.UnifiedQuery,
                domainEvent.SearchCriteria.Isrc,
                domainEvent.SearchCriteria.Title,
                domainEvent.SearchCriteria.Artist,
                domainEvent.SearchCriteria.Album,
                domainEvent.Hierarchy?.ArtistId?.Value,
                domainEvent.Hierarchy?.AlbumId?.Value),
            dto => new StreamingLocationsRequired(
                MusicCatalogId.From(dto.MusicCatalogId),
                Enum.Parse<LookupPriorityBand>(dto.Priority, ignoreCase: true),
                CorrelationId.From(dto.CorrelationId),
                LookupSource.From(dto.SourceProvider),
                dto.ObservedAt,
                dto.SearchKind switch
                {
                    MusicSearchKind.UnifiedSearch => MusicSearchCriteria.ByQuery(
                        dto.Query ?? throw new InvalidOperationException("Stored unified streaming locations event requires a query.")),
                    MusicSearchKind.Isrc => MusicSearchCriteria.ByIsrc(
                        dto.Isrc ?? throw new InvalidOperationException("Stored ISRC streaming locations event requires an ISRC.")),
                    MusicSearchKind.TrackArtistAlbum => MusicSearchCriteria.ByTrackArtistAlbum(
                        dto.Title ?? throw new InvalidOperationException("Stored track/artist/album streaming locations event requires a title."),
                        dto.Artist ?? throw new InvalidOperationException("Stored track/artist/album streaming locations event requires an artist."),
                        dto.Album),
                    _ => throw new InvalidOperationException($"Unsupported music search kind '{dto.SearchKind}'.")
                },
                dto.ArtistId is null && dto.AlbumId is null
                    ? null
                    : new CatalogTrackHierarchy(
                        dto.ArtistId is null ? null : ArtistId.From(dto.ArtistId),
                        dto.AlbumId is null ? null : AlbumId.From(dto.AlbumId))),
            domainEvent => domainEvent.ObservedAt,
            domainEvent => domainEvent.CorrelationId.Value);
    }
}
