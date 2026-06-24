using Raven.Client.Documents.Session;
using Soundtrail.Contracts.EventSourcing;
using Soundtrail.Domain.Discovery;
using Soundtrail.Services.Internal.Projector.Features.OnCatalogSearchStatusChanged.Adapters;
using Soundtrail.Services.Internal.Projector.Features.OnReplayCatalogSearchStatus.StoredEvents;

namespace Soundtrail.Services.Internal.Projector.Features.OnReplayCatalogSearchStatus.Adapters;

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
