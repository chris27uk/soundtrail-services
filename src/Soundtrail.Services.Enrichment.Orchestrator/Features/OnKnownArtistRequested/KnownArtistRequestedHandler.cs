using Soundtrail.Domain.Abstractions;
using Soundtrail.Domain.Abstractions.EventSourcing;
using Soundtrail.Domain.Discovery;
using Soundtrail.Domain.Discovery.Commands;
using Soundtrail.Domain.Search;

namespace Soundtrail.Services.Enrichment.Orchestrator.Features.OnKnownArtistRequested;

public sealed class KnownArtistRequestedHandler(IEventStreamRepository<DiscoveryQueryKey, IDomainEvent> discoveryRepository) : IHandler<KnownArtistRequested>
{
    public async Task Handle(
        KnownArtistRequested request,
        CancellationToken cancellationToken = default)
    {
        await SearchOrSeekHistory.ArtistCatalogLookupRequestedAsync(
            discoveryRepository,
            request.ArtistId,
            request.OccurredAt,
            request.CorrelationId,
            cancellationToken);
    }
}
