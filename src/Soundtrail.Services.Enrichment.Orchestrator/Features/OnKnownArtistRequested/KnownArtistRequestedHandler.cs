using Soundtrail.Contracts.Common;
using Soundtrail.Domain.Abstractions;
using Soundtrail.Domain.Abstractions.EventSourcing;
using Soundtrail.Domain.Discovery;
using Soundtrail.Domain.Discovery.Commands;
using Soundtrail.Domain.Search;

namespace Soundtrail.Services.Enrichment.Orchestrator.Features.OnKnownArtistRequested;

public sealed class KnownArtistRequestedHandler(
    IEventStreamRepository<DiscoveryQueryKey, IDomainEvent> discoveryRepository) : IHandler<KnownArtistRequested>
{
    public async Task Handle(
        KnownArtistRequested request,
        CancellationToken cancellationToken = default)
    {
        var loaded = await KnownItemDiscovery.LoadAsync(
            discoveryRepository,
            KnownCatalogItem.ForArtist(request.ArtistId),
            cancellationToken);

        loaded.Aggregate.ArtistRequested(
                request.ArtistId,
                request.OccurredAt,
                request.CorrelationId);

        await loaded.Aggregate.SaveAsync(discoveryRepository, loaded.Stream, cancellationToken);
    }
}
