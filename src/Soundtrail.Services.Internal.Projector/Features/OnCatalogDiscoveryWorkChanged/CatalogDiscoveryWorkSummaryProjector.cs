using Soundtrail.Contracts.Common;
using Soundtrail.Domain.Abstractions.EventSourcing;
using Soundtrail.Domain.Discovery;

namespace Soundtrail.Services.Internal.Projector.Features.OnCatalogDiscoveryWorkChanged;

public sealed class CatalogDiscoveryWorkSummaryProjector(ICatalogDiscoveryWorkSummaryStore store)
{
    public async Task ProjectAsync(
        MusicCatalogId musicCatalogId,
        IReadOnlyList<(int Version, IDomainEvent Event)> events,
        CancellationToken cancellationToken)
    {
        var snapshot = await store.LoadAsync(musicCatalogId, cancellationToken);
        var projection = snapshot is null
            ? new LatestDiscoveryWorkProjection(musicCatalogId)
            : LatestDiscoveryWorkProjection.Load(snapshot);

        foreach (var @event in events)
        {
            projection.Apply(@event.Event, @event.Version);
        }

        await store.SaveAsync(projection.ToSnapshot(), cancellationToken);
    }
}
