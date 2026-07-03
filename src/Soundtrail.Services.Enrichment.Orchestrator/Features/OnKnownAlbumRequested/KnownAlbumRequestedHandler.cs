using Soundtrail.Contracts.Common;
using Soundtrail.Domain.Abstractions;
using Soundtrail.Domain.Abstractions.EventSourcing;
using Soundtrail.Domain.Discovery;
using Soundtrail.Domain.Discovery.Commands;
using Soundtrail.Domain.Search;

namespace Soundtrail.Services.Enrichment.Orchestrator.Features.OnKnownAlbumRequested;

public sealed class KnownAlbumRequestedHandler(
    IEventStreamRepository<DiscoveryQueryKey, IDomainEvent> discoveryRepository) : IHandler<KnownAlbumRequested>
{
    public async Task Handle(
        KnownAlbumRequested request,
        CancellationToken cancellationToken = default)
    {
        var catalogId = KnownCatalogId.ForAlbum(request.ArtistId, request.AlbumId);
        var loaded = await KnownItemDiscovery.LoadAsync(discoveryRepository, catalogId, cancellationToken);

        loaded.Aggregate.AlbumRequested(
                request.ArtistId,
                request.AlbumId,
                request.OccurredAt,
                request.CorrelationId);
        
        await loaded.Aggregate.SaveAsync(discoveryRepository, loaded.Stream, cancellationToken);
    }
}
