using Soundtrail.Domain.Discovery;
using Soundtrail.Domain.Discovery.Commands;

namespace Soundtrail.Services.Enrichment.Orchestrator.Features.OnKnownTrackRequested;

public sealed class KnownTrackRequestedHandler(ICatalogSearchDiscoveryRepository discoveryRepository)
{
    public async Task Handle(
        KnownTrackRequested request,
        CancellationToken cancellationToken = default)
    {
        var history = await SearchOrSeekHistory.LoadAsync(
            discoveryRepository,
            KnownCatalogItem.ForTrack(request.TrackId),
            cancellationToken);

        history.KnownTrackRequested(
            request.TrackId,
            request.Playback,
            request.OccurredAt,
            request.CorrelationId);

        await history.SaveAsync(discoveryRepository, cancellationToken);
    }
}
