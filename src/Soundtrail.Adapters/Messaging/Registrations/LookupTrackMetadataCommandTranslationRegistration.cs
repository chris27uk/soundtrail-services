using Soundtrail.Contracts.IntegrationMessaging.Commands;
using Soundtrail.Contracts.Common;
using Soundtrail.Domain.Catalog;
using Soundtrail.Domain.Enrichment.Commands;
using Soundtrail.Domain.Search;
using Soundtrail.Adapters.Registry;

namespace Soundtrail.Adapters.Messaging.Registrations;

public sealed class LookupTrackMetadataCommandTranslationRegistration : ITypeTranslationRegistration
{
    public void Register(TypeTranslationRegistry registry)
    {
        registry.RegisterPair<LookupTrackMetadataCommand, LookupTrackMetadataCommandDto>(
            command =>
                new LookupTrackMetadataCommandDto(
                    command.CommandId.Value,
                    command.MusicCatalogId.Value,
                    command.Priority,
                    command.CreatedAt,
                    command.CorrelationId.Value,
                    command.SearchCriteria.Kind,
                    command.SearchCriteria.UnifiedQuery,
                    command.SearchCriteria.Isrc,
                    command.SearchCriteria.Title,
                    command.SearchCriteria.Artist,
                    command.SearchCriteria.Album,
                    command.Hierarchy?.ArtistId?.Value,
                    command.Hierarchy?.AlbumId?.Value),
            dto =>
                new LookupTrackMetadataCommand(
                    CommandId.From(dto.CommandId),
                    MusicCatalogId.From(dto.MusicCatalogId),
                    dto.Priority,
                    dto.CreatedAt,
                    CorrelationId.From(dto.CorrelationId),
                    dto.SearchKind switch
                    {
                        MusicSearchKind.UnifiedSearch => LookupCriteria.Query(
                            dto.Query ?? throw new InvalidOperationException("Unified music metadata lookup requires a query.")),
                        MusicSearchKind.Isrc => LookupCriteria.ExactIsrc(
                            dto.Isrc ?? throw new InvalidOperationException("ISRC music metadata lookup requires an ISRC.")),
                        MusicSearchKind.TrackArtistAlbum => LookupCriteria.ByTrackArtistAlbum(
                            dto.TrackName ?? throw new InvalidOperationException("Track/artist/album music metadata lookup requires a track name."),
                            dto.ArtistName ?? throw new InvalidOperationException("Track/artist/album music metadata lookup requires an artist name."),
                            dto.AlbumName),
                        _ => throw new InvalidOperationException($"Unsupported music search kind '{dto.SearchKind}'.")
                    },
                    dto.ArtistId is null && dto.AlbumId is null
                        ? null
                        : new CatalogTrackHierarchy(
                            dto.ArtistId is null ? null : ArtistId.From(dto.ArtistId),
                            dto.AlbumId is null ? null : AlbumId.From(dto.AlbumId))));
    }
}
