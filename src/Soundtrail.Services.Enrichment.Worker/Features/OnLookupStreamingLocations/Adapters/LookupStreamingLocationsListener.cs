using Raven.Client.Documents.Session;
using Soundtrail.Contracts.Common;
using Soundtrail.Contracts.IntegrationMessaging.Commands;
using Soundtrail.Domain.Abstractions;
using Soundtrail.Domain.Catalog;
using Soundtrail.Domain.Search;
using Wolverine.Attributes;

namespace Soundtrail.Services.Enrichment.Worker.Features.OnLookupStreamingLocations.Adapters;

public sealed class LookupStreamingLocationsListener(IHandler<LookupStreamingLocationsCommand> handler)
{
    [WolverineHandler]
    [Transactional]
    public async Task Handle(
        LookupStreamingLocationsCommandDto dto,
        IAsyncDocumentSession _,
        CancellationToken cancellationToken = default)
    {
        var command = new LookupStreamingLocationsCommand(
            CommandId.From(dto.CommandId),
            MusicCatalogId.From(dto.MusicCatalogId),
            dto.Priority,
            dto.CreatedAt,
            CorrelationId.From(dto.CorrelationId),
            ToSearchTerm(dto.SearchTerm),
            ToHierarchy(dto));
        await handler.Handle(command, cancellationToken);
    }

    private static LookupCriteria ToSearchTerm(StreamingLocationSearchTermDto dto) =>
        dto.Kind switch
        {
            MusicSearchKind.UnifiedSearch => LookupCriteria.Query(
                dto.Query ?? throw new InvalidOperationException("Unified streaming locations lookup requires a query.")),
            MusicSearchKind.Isrc => LookupCriteria.(
                dto.Isrc ?? throw new InvalidOperationException("ISRC streaming locations lookup requires an ISRC.")),
            MusicSearchKind.TrackArtistAlbum => LookupCriteria.ByTrackArtistAlbum(
                dto.Title ?? throw new InvalidOperationException("Track/artist/album streaming locations lookup requires a title."),
                dto.Artist ?? throw new InvalidOperationException("Track/artist/album streaming locations lookup requires an artist."),
                dto.Album),
            _ => throw new InvalidOperationException($"Unsupported music search kind '{dto.Kind}'.")
        };

    private static CatalogTrackHierarchy? ToHierarchy(LookupStreamingLocationsCommandDto dto) =>
        dto.ArtistId is null && dto.AlbumId is null
            ? null
            : new CatalogTrackHierarchy(
                dto.ArtistId is null ? null : ArtistId.From(dto.ArtistId),
                dto.AlbumId is null ? null : AlbumId.From(dto.AlbumId));
}
