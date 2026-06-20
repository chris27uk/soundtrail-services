using Raven.Client.Documents.Session;
using Soundtrail.Contracts.EventSourcing;
using Soundtrail.Domain.Discovery;
using Soundtrail.Services.Enrichment.DiscoveryPlanner.Features.ProjectDiscoveryLifecycle.Adapters;
using Soundtrail.Services.Enrichment.DiscoveryPlanner.Features.ReplayDiscoveryLifecycleProjection.StoredEvents;

namespace Soundtrail.Services.Enrichment.DiscoveryPlanner.Features.ReplayDiscoveryLifecycleProjection.Adapters;

public sealed class RavenLoadStoredDiscoveryLifecycleEvents(
    IAsyncDocumentSession session) : ILoadStoredDiscoveryLifecycleEventsPort
{
    public async Task<IReadOnlyList<VersionedCatalogSearchDiscoveryEvent>> LoadAsync(
        CatalogSearchCriteria criteria,
        CancellationToken cancellationToken)
    {
        var events = await session.Advanced.AsyncDocumentQuery<DiscoveryQueryStoredEventRecordDto>()
            .WhereEquals(nameof(DiscoveryQueryStoredEventRecordDto.Criteria), criteria.Value)
            .ToListAsync(cancellationToken);

        return events
            .OrderBy(x => x.Version)
            .Select(item => item.ToDomainEvent())
            .ToArray();
    }
}
