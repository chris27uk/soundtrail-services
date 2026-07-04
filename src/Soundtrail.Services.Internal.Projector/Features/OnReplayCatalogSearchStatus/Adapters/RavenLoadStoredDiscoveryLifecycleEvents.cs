using Raven.Client.Documents.Session;
using Soundtrail.Contracts.EventSourcing;
using Soundtrail.Domain.Abstractions.EventSourcing;
using Soundtrail.Domain.Discovery;
using Soundtrail.Domain.Search;
using Soundtrail.Services.Internal.Projector.Features.OnReplayCatalogSearchStatus.StoredEvents;
using Soundtrail.Adapters.Registry;

namespace Soundtrail.Services.Internal.Projector.Features.OnReplayCatalogSearchStatus.Adapters;

public sealed class RavenLoadStoredDiscoveryLifecycleEvents(
    IAsyncDocumentSession session,
    ITypeRegistry registry) : ILoadStoredDiscoveryLifecycleEventsPort
{
    public async Task<IReadOnlyList<VersionedCatalogSearchDiscoveryEvent>> LoadAsync(
        LookupCriteria searchCriteria,
        CancellationToken cancellationToken)
    {
        var persistentId = DiscoveryQueryKey.StableValueFor(searchCriteria);
        var events = (await session.Advanced.LoadStartingWithAsync<RavenStoredEventRecord>(
                $"discovery-query-events/{persistentId}/"))
            .ToList();

        return events
            .OrderBy(x => x.Version)
            .Select(item => new VersionedCatalogSearchDiscoveryEvent(item.Version, registry.ToDomainObject<IDomainEvent>(
                item.Body ?? throw new InvalidOperationException($"Stored event '{item.Id}' is missing a body."))))
            .ToArray();
    }
}
