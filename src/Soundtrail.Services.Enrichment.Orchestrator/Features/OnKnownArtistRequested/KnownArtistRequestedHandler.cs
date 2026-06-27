using Soundtrail.Domain.Abstractions;
using Soundtrail.Domain.Discovery;
using Soundtrail.Domain.Discovery.Commands;

namespace Soundtrail.Services.Enrichment.Orchestrator.Features.OnKnownArtistRequested;

public sealed class KnownArtistRequestedHandler(ICatalogSearchDiscoveryRepository discoveryRepository) : IHandler<KnownArtistRequested>
{
    public async Task Handle(
        KnownArtistRequested request,
        CancellationToken cancellationToken = default)
    {
        var history = await SearchOrSeekHistory.LoadAsync(
            discoveryRepository,
            KnownCatalogItem.ForArtist(request.ArtistId),
            cancellationToken);

        history.ArtistCatalogLookupRequested(
            request.ArtistId,
            request.OccurredAt,
            request.CorrelationId);

        await history.SaveAsync(discoveryRepository, cancellationToken);
    }
}
