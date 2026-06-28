using Soundtrail.Domain.Abstractions;
using Soundtrail.Domain.Abstractions.EventSourcing;
using Soundtrail.Domain.Discovery;
using Soundtrail.Domain.Discovery.Commands;
using Soundtrail.Domain.Search;

namespace Soundtrail.Services.Enrichment.Orchestrator.Features.OnKnownAlbumRequested;

public sealed class KnownAlbumRequestedHandler(IEventStreamRepository<DiscoveryQueryKey, IDomainEvent> discoveryRepository) : IHandler<KnownAlbumRequested>
{
    public async Task Handle(
        KnownAlbumRequested request,
        CancellationToken cancellationToken = default)
    {
        await SearchOrSeekHistory.AlbumCatalogLookupRequestedAsync(
            discoveryRepository,
            null,
            request.AlbumId,
            request.OccurredAt,
            request.CorrelationId,
            cancellationToken);
    }
}
