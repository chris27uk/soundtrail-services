using Raven.Client.Documents.Session;
using Soundtrail.Contracts.EventSourcing;
using Soundtrail.Domain.Discovery;
using Soundtrail.Domain.Search;
using Soundtrail.Services.Internal.Projector.Features.OnReplayCatalogSearchStatus.StoredEvents;
using Soundtrail.Adapters.Discovery;

namespace Soundtrail.Services.Internal.Projector.Features.OnReplayCatalogSearchStatus.Adapters;

public sealed class RavenLoadStoredDiscoveryLifecycleEvents(
    IAsyncDocumentSession session) : ILoadStoredDiscoveryLifecycleEventsPort
{
    public async Task<IReadOnlyList<VersionedCatalogSearchDiscoveryEvent>> LoadAsync(
        MusicSearchCriteria searchCriteria,
        CancellationToken cancellationToken)
    {
        var persistentId = DiscoveryQueryKey.StableValueFor(searchCriteria);
        var events = (await session.Advanced.LoadStartingWithAsync<DiscoveryQueryStoredEventRecordDto>(
                $"discovery-query-events/{persistentId}/"))
            .ToList();

        return events
            .OrderBy(x => x.Version)
            .Select(item => item.ToDomainEvent())
            .ToArray();
    }
}
