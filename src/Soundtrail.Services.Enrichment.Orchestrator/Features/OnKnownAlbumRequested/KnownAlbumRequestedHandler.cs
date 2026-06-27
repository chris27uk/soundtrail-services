using Soundtrail.Domain.Abstractions;
using Soundtrail.Domain.Discovery;
using Soundtrail.Domain.Discovery.Commands;

namespace Soundtrail.Services.Enrichment.Orchestrator.Features.OnKnownAlbumRequested;

public sealed class KnownAlbumRequestedHandler(ICatalogSearchDiscoveryRepository discoveryRepository) : IHandler<KnownAlbumRequested>
{
    public async Task Handle(
        KnownAlbumRequested request,
        CancellationToken cancellationToken = default)
    {
        var history = await SearchOrSeekHistory.LoadAsync(
            discoveryRepository,
            KnownCatalogItem.ForAlbum(request.AlbumId),
            cancellationToken);

        history.AlbumCatalogLookupRequested(
            null,
            request.AlbumId,
            request.OccurredAt,
            request.CorrelationId);

        await history.SaveAsync(discoveryRepository, cancellationToken);
    }
}
