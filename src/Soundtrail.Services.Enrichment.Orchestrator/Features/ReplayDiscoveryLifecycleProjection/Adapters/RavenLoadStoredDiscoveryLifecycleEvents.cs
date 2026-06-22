using Raven.Client.Documents.Session;
using Soundtrail.Contracts.EventSourcing;
using Soundtrail.Domain.Discovery;
using Soundtrail.Services.Enrichment.Orchestrator.Features.ProjectDiscoveryLifecycle.Adapters;
using Soundtrail.Services.Enrichment.Orchestrator.Features.ReplayDiscoveryLifecycleProjection.StoredEvents;

namespace Soundtrail.Services.Enrichment.Orchestrator.Features.ReplayDiscoveryLifecycleProjection.Adapters;

public sealed class RavenLoadStoredDiscoveryLifecycleEvents(
    IAsyncDocumentSession session) : ILoadStoredDiscoveryLifecycleEventsPort
{
    public async Task<IReadOnlyList<VersionedCatalogSearchDiscoveryEvent>> LoadAsync(
        CatalogSearchCriteria criteria,
        CancellationToken cancellationToken)
    {
        var events = (await session.Advanced.LoadStartingWithAsync<DiscoveryQueryStoredEventRecordDto>(
                $"discovery-query-events/{criteria.Value}/"))
            .ToList();

        return events
            .OrderBy(x => x.Version)
            .Select(item => item.ToDomainEvent())
            .ToArray();
    }
}
