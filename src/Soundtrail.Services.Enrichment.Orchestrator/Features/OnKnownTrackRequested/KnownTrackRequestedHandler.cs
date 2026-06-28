using Soundtrail.Domain.Abstractions;
using Soundtrail.Domain.Abstractions.EventSourcing;
using Soundtrail.Domain.Discovery;
using Soundtrail.Domain.Discovery.Commands;
using Soundtrail.Domain.Search;

namespace Soundtrail.Services.Enrichment.Orchestrator.Features.OnKnownTrackRequested;

public sealed class KnownTrackRequestedHandler(IEventStreamRepository<DiscoveryQueryKey, IDomainEvent> discoveryRepository) : IHandler<KnownTrackRequested>
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
