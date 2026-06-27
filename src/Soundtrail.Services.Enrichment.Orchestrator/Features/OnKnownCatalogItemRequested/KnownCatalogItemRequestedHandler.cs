using Soundtrail.Contracts.Common;
using Soundtrail.Domain.Catalog;
using Soundtrail.Domain.Discovery;
using Soundtrail.Domain.Discovery.Commands;
using Soundtrail.Services.Enrichment.Orchestrator.Features.OnKnownCatalogItemRequested.Ports;

namespace Soundtrail.Services.Enrichment.Orchestrator.Features.OnKnownCatalogItemRequested;

public sealed class KnownCatalogItemRequestedHandler(
    ICatalogSearchDiscoveryRepository discoveryRepository,
    ILoadKnownCatalogTrackPort loadKnownCatalogTrackPort)
{
    public async Task Handle(
        KnownCatalogItemRequested request,
        CancellationToken cancellationToken = default)
    {
        var history = await SearchOrSeekHistory.LoadAsync(
            discoveryRepository,
            request.KnownItem,
            cancellationToken);

        switch (request.KnownItem)
        {
            case { ArtistId: not null }:
                history.ArtistCatalogLookupRequested(
                    request.KnownItem.ArtistId.Value,
                    request.OccurredAt,
                    request.CorrelationId);
                break;
            case { AlbumId: not null }:
                history.AlbumCatalogLookupRequested(
                    request.KnownItem.ArtistId,
                    request.KnownItem.AlbumId.Value,
                    request.OccurredAt,
                    request.CorrelationId);
                break;
            case { TrackId: not null }:
                var track = await loadKnownCatalogTrackPort.LoadAsync(request.KnownItem.TrackId.Value, cancellationToken);
                if (track is not null
                    && track.CanCreateSearchTerm()
                    && track.RequiresStreamingLocations(request.Playback))
                {
                    history.StreamingLocationsRequired(
                        track.MusicCatalogId,
                        LookupPriorityBand.Low,
                        request.OccurredAt,
                        request.CorrelationId,
                        track.ToSearchTerm(),
                        track.ArtistId is null && track.AlbumId is null
                            ? null
                            : new CatalogTrackHierarchy(track.ArtistId, track.AlbumId));
                }
                break;
        }

        await history.SaveAsync(discoveryRepository, cancellationToken);
    }
}
