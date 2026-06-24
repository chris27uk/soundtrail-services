using Raven.Client.Documents.Session;
using Soundtrail.Contracts.Common;
using Soundtrail.Contracts.IntegrationMessaging.Events;
using Soundtrail.Domain.Catalog;
using Soundtrail.Domain.Model;
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

    private static MusicSearchTerm ToSearchTerm(StreamingLocationsRequiredMessageDto dto) =>
        !string.IsNullOrWhiteSpace(dto.SearchTerm.Isrc)
            ? MusicSearchTerm.ByIsrc(dto.SearchTerm.Isrc)
            : MusicSearchTerm.ByTrackArtistAlbum(
                dto.SearchTerm.Title ?? throw new InvalidOperationException("Streaming locations lookup requires a title when no ISRC is present."),
                dto.SearchTerm.Artist ?? throw new InvalidOperationException("Streaming locations lookup requires an artist when no ISRC is present."),
                dto.SearchTerm.Album);
}
