using Soundtrail.Contracts.IntegrationMessaging.Commands;
using Soundtrail.Contracts.Common;
using Soundtrail.Domain.Catalog;
using Soundtrail.Domain.Enrichment.Commands;
using Soundtrail.Domain.Search;
using Soundtrail.Adapters.Registry;

namespace Soundtrail.Adapters.Messaging.Registrations;

public sealed class LookupStreamingLocationsCommandTranslationRegistration : ITypeTranslationRegistration
{
    public void Register(TypeTranslationRegistry registry)
    {
        registry.RegisterPair<LookupStreamingLocationsCommand, LookupStreamingLocationsCommandDto>(
            command =>
                new LookupStreamingLocationsCommandDto(
                    command.CommandId.Value,
                    command.MusicCatalogId.Value,
                    command.Priority,
                    command.CreatedAt,
                    command.CorrelationId.Value,
                    new StreamingLocationSearchTermDto(
                        command.LookupKey.Kind,
                        command.LookupKey.UnifiedQuery,
                        command.LookupKey.Isrc,
                        command.LookupKey.Title,
                        command.LookupKey.Artist,
                        command.LookupKey.Album),
                    command.Hierarchy?.ArtistId?.Value,
                    command.Hierarchy?.AlbumId?.Value),
            dto =>
                new LookupStreamingLocationsCommand(
                    CommandId.From(dto.CommandId),
                    MusicCatalogId.From(dto.MusicCatalogId),
                    dto.Priority,
                    dto.CreatedAt,
                    CorrelationId.From(dto.CorrelationId),
                    dto.SearchTerm.Kind switch
                    {
                        MusicSearchKind.UnifiedSearch => MusicSearchCriteria.ByQuery(
                            dto.SearchTerm.Query ?? throw new InvalidOperationException("Unified streaming locations lookup requires a query.")),
                        MusicSearchKind.Isrc => MusicSearchCriteria.ByIsrc(
                            dto.SearchTerm.Isrc ?? throw new InvalidOperationException("ISRC streaming locations lookup requires an ISRC.")),
                        MusicSearchKind.TrackArtistAlbum => MusicSearchCriteria.ByTrackArtistAlbum(
                            dto.SearchTerm.Title ?? throw new InvalidOperationException("Track/artist/album streaming locations lookup requires a title."),
                            dto.SearchTerm.Artist ?? throw new InvalidOperationException("Track/artist/album streaming locations lookup requires an artist."),
                            dto.SearchTerm.Album),
                        _ => throw new InvalidOperationException($"Unsupported music search kind '{dto.SearchTerm.Kind}'.")
                    },
                    dto.ArtistId is null && dto.AlbumId is null
                        ? null
                        : new CatalogTrackHierarchy(
                            dto.ArtistId is null ? null : ArtistId.From(dto.ArtistId),
                            dto.AlbumId is null ? null : AlbumId.From(dto.AlbumId))));
    }
}
