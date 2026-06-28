using Soundtrail.Contracts.Common;
using Soundtrail.Contracts.EventSourcing;
using Soundtrail.Domain.Abstractions.EventSourcing;
using Soundtrail.Domain.Discovery;
using Soundtrail.Adapters.Registry;

namespace Soundtrail.Services.Enrichment.Orchestrator.Features.OnCatalogDiscoveryWorkChanged;

public sealed class CatalogDiscoveryWorkSummaryProjector(
    ICatalogDiscoveryWorkSummaryStore store,
    ITypeRegistry registry)
{
    public async Task ProjectAsync(
        MusicCatalogId musicCatalogId,
        IReadOnlyList<RavenStoredEventRecord> storedEvents,
        CancellationToken cancellationToken)
    {
        var snapshot = await store.LoadAsync(musicCatalogId, cancellationToken);
        var projection = snapshot is null
            ? new CatalogDiscoveryWorkSummaryProjection(musicCatalogId)
            : CatalogDiscoveryWorkSummaryProjection.Load(snapshot);

        foreach (var storedEvent in storedEvents.OrderBy(x => x.Version))
        {
            projection.Apply(
                registry.ToDomainObject<IDomainEvent>(
                    storedEvent.Body ?? throw new InvalidOperationException($"Stored event '{storedEvent.Id}' is missing a body.")),
                storedEvent.Version);
        }

        await store.SaveAsync(projection.ToSnapshot(), cancellationToken);
    }
}
