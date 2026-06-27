using Raven.Client.Documents.Session;
using Soundtrail.Contracts.Common;
using Soundtrail.Contracts.IntegrationMessaging.Events;
using Soundtrail.Domain.Catalog;
using Soundtrail.Domain.Search;
using Soundtrail.Services.Enrichment.Orchestrator.Features.OnStreamingLocationsRequired.Support;
using Wolverine.Attributes;

namespace Soundtrail.Services.Enrichment.Orchestrator.Features.OnStreamingLocationsRequired.Adapters;

public sealed class StreamingLocationsRequiredListener(StreamingLocationsRequiredHandler handler)
{
    [WolverineHandler]
    [Transactional]
    public Task Handle(
        StreamingLocationsRequiredMessageDto dto,
        IAsyncDocumentSession _,
        CancellationToken cancellationToken = default)
    {
        return handler.Handle(
            new ScheduleStreamingLocationsLookupCommand(
                MusicCatalogId.From(dto.MusicCatalogId),
                dto.Priority,
                dto.ObservedAt,
                CorrelationId.From(dto.CorrelationId),
                ToSearchTerm(dto),
                dto.ArtistId is null ? null : ArtistId.From(dto.ArtistId),
                dto.AlbumId is null ? null : AlbumId.From(dto.AlbumId)),
            cancellationToken);
    }

    private static MusicSearchCriteria ToSearchTerm(StreamingLocationsRequiredMessageDto dto) =>
        dto.SearchTerm.Kind switch
        {
            MusicSearchKind.UnifiedSearch => MusicSearchCriteria.ByQuery(
                dto.SearchTerm.Query ?? throw new InvalidOperationException("Streaming locations lookup requires a query for unified search.")),
            MusicSearchKind.Isrc => MusicSearchCriteria.ByIsrc(
                dto.SearchTerm.Isrc ?? throw new InvalidOperationException("Streaming locations lookup requires an ISRC for ISRC lookups.")),
            MusicSearchKind.TrackArtistAlbum => MusicSearchCriteria.ByTrackArtistAlbum(
                dto.SearchTerm.Title ?? throw new InvalidOperationException("Streaming locations lookup requires a title for track/artist/album lookups."),
                dto.SearchTerm.Artist ?? throw new InvalidOperationException("Streaming locations lookup requires an artist for track/artist/album lookups."),
                dto.SearchTerm.Album),
            _ => throw new InvalidOperationException($"Unsupported music search kind '{dto.SearchTerm.Kind}'.")
        };
}
