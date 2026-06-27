using Raven.Client.Documents.Session;
using Soundtrail.Contracts.Common;
using Soundtrail.Contracts.IntegrationMessaging.Commands;
using Soundtrail.Domain.Catalog;
using Soundtrail.Domain.Discovery.Commands;
using Soundtrail.Domain.Search;
using Soundtrail.Services.Enrichment.Orchestrator.Features.OnKnownAlbumRequested;
using Soundtrail.Services.Enrichment.Orchestrator.Features.OnKnownArtistRequested;
using Soundtrail.Services.Enrichment.Orchestrator.Features.OnKnownTrackRequested;
using Wolverine.Attributes;

namespace Soundtrail.Services.Enrichment.Orchestrator.Features.OnKnownCatalogItemRequested.Adapters;

public sealed class KnownCatalogItemRequestedListener(
    KnownArtistRequestedHandler knownArtistRequestedHandler,
    KnownAlbumRequestedHandler knownAlbumRequestedHandler,
    KnownTrackRequestedHandler knownTrackRequestedHandler)
{
    [WolverineHandler]
    [Transactional]
    public Task Handle(
        KnownCatalogItemRequestedDto dto,
        IAsyncDocumentSession _,
        CancellationToken cancellationToken = default) =>
        dto switch
        {
            { ArtistId: not null } => knownArtistRequestedHandler.Handle(
                ToKnownArtistRequested(dto),
                cancellationToken),
            { AlbumId: not null } => knownAlbumRequestedHandler.Handle(
                ToKnownAlbumRequested(dto),
                cancellationToken),
            { TrackId: not null } => knownTrackRequestedHandler.Handle(
                ToKnownTrackRequested(dto),
                cancellationToken),
            _ => throw new InvalidOperationException("Known catalog item request must contain an artist id, album id or track id.")
        };
    
    private static KnownArtistRequested ToKnownArtistRequested(KnownCatalogItemRequestedDto dto) =>
        new(
            ArtistId.From(dto.ArtistId ?? throw new InvalidOperationException("Known artist request must contain an artist id.")),
            dto.OccurredAt,
            CorrelationId.From(dto.CorrelationId));
    
    private static KnownAlbumRequested ToKnownAlbumRequested(KnownCatalogItemRequestedDto dto) =>
        new(
            AlbumId.From(dto.AlbumId ?? throw new InvalidOperationException("Known album request must contain an album id.")),
            dto.OccurredAt,
            CorrelationId.From(dto.CorrelationId));

    private static KnownTrackRequested ToKnownTrackRequested(KnownCatalogItemRequestedDto dto) =>
        new(
            TrackId.From(dto.TrackId ?? throw new InvalidOperationException("Known track request must contain a track id.")),
            PlaybackProviderFilter.Parse(dto.Playback),
            dto.OccurredAt,
            CorrelationId.From(dto.CorrelationId));
}
