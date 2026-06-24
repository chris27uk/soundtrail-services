using Soundtrail.Contracts.Common;
using Soundtrail.Contracts.EventSourcing;
using Soundtrail.Domain.Catalog;
using Soundtrail.Domain.Events;
using Soundtrail.Domain.Model;

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
                domainEvent.SearchTerm.Kind,
                domainEvent.SearchTerm.Query,
                domainEvent.SearchTerm.Isrc,
                domainEvent.SearchTerm.Title,
                domainEvent.SearchTerm.Artist,
                domainEvent.SearchTerm.Album,
                domainEvent.Hierarchy?.ArtistId?.Value,
                domainEvent.Hierarchy?.AlbumId?.Value),
            dto => new StreamingLocationsRequired(
                MusicCatalogId.From(dto.MusicCatalogId),
                Enum.Parse<LookupPriorityBand>(dto.Priority, ignoreCase: true),
                CorrelationId.From(dto.CorrelationId),
                ProviderName.From(dto.SourceProvider),
                dto.ObservedAt,
                dto.SearchKind switch
                {
                    MusicSearchKind.UnifiedSearch => MusicSearchTerm.ByQuery(
                        dto.Query ?? throw new InvalidOperationException("Stored unified streaming locations event requires a query.")),
                    MusicSearchKind.Isrc => MusicSearchTerm.ByIsrc(
                        dto.Isrc ?? throw new InvalidOperationException("Stored ISRC streaming locations event requires an ISRC.")),
                    MusicSearchKind.TrackArtistAlbum => MusicSearchTerm.ByTrackArtistAlbum(
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
