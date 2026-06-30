using Raven.Client.Documents.Session;
using Soundtrail.Contracts.Common;
using Soundtrail.Contracts.IntegrationMessaging.Commands;
using Soundtrail.Domain.Abstractions;
using Soundtrail.Domain.Catalog;
using Soundtrail.Domain.Enrichment.Commands;
using Soundtrail.Domain.Search;
using Wolverine.Attributes;

namespace Soundtrail.Services.Enrichment.Worker.Features.OnLookupTrackMetadata.Adapters;

public sealed class LookupTrackMetadataListener(IHandler<LookupTrackMetadataCommand> handler)
{
    [WolverineHandler]
    [Transactional]
    public async Task Handle(
        LookupTrackMetadataCommandDto dto,
        IAsyncDocumentSession _,
        CancellationToken cancellationToken = default)
    {
        var command = new LookupTrackMetadataCommand(
            CommandId.From(dto.CommandId),
            MusicCatalogId.From(dto.MusicCatalogId),
            dto.Priority,
            dto.CreatedAt,
            CorrelationId.From(dto.CorrelationId),
            ToSearchTerm(dto),
            ToHierarchy(dto));
        await handler.Handle(command, cancellationToken);
    }

    private static MusicSearchCriteria ToSearchTerm(LookupTrackMetadataCommandDto dto) =>
        dto.SearchKind switch
        {
            MusicSearchKind.UnifiedSearch => MusicSearchCriteria.ByQuery(
                dto.Query ?? throw new InvalidOperationException("Unified music metadata lookup requires a query.")),
            MusicSearchKind.Isrc => MusicSearchCriteria.ByIsrc(
                dto.Isrc ?? throw new InvalidOperationException("ISRC music metadata lookup requires an ISRC.")),
            MusicSearchKind.TrackArtistAlbum => MusicSearchCriteria.ByTrackArtistAlbum(
                dto.TrackName ?? throw new InvalidOperationException("Track/artist/album music metadata lookup requires a track name."),
                dto.ArtistName ?? throw new InvalidOperationException("Track/artist/album music metadata lookup requires an artist name."),
                dto.AlbumName),
            _ => throw new InvalidOperationException($"Unsupported music search kind '{dto.SearchKind}'.")
        };

    private static CatalogTrackHierarchy? ToHierarchy(LookupTrackMetadataCommandDto dto) =>
        dto.ArtistId is null && dto.AlbumId is null
            ? null
            : new CatalogTrackHierarchy(
                dto.ArtistId is null ? null : ArtistId.From(dto.ArtistId),
                dto.AlbumId is null ? null : AlbumId.From(dto.AlbumId));
}
