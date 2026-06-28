using Raven.Client.Documents.Session;
using Soundtrail.Adapters.Registry;
using Soundtrail.Contracts.EventSourcing;
using Soundtrail.Domain.Abstractions.EventSourcing;
using Soundtrail.Domain.Discovery;
using Soundtrail.Domain.Search;
using Soundtrail.Services.Internal.Projector.Features.OnCatalogSearchStatusChanged.Adapters;
using Soundtrail.Tools.MusicBrainzImport.Features.ReplayDiscoveryLifecycleProjection.EventStore;

namespace Soundtrail.Tools.MusicBrainzImport.Features.ReplayDiscoveryLifecycleProjection.Adapters;

public sealed class RavenLoadDiscoveryLifecycleEventsForReplay(
    IAsyncDocumentSession session,
    ITypeRegistry registry) : ILoadDiscoveryLifecycleEventsForReplayPort
{
    public async Task<IReadOnlyList<VersionedCatalogSearchDiscoveryEvent>> LoadAsync(
        MusicSearchCriteria searchCriteria,
        CancellationToken cancellationToken)
    {
        var persistentId = DiscoveryQueryKey.StableValueFor(searchCriteria);
        var events = (await session.Advanced.LoadStartingWithAsync<RavenStoredEventRecord>(
                $"discovery-query-events/{persistentId}/"))
            .OrderBy(x => x.Version)
            .Select(x => new VersionedCatalogSearchDiscoveryEvent(
                x.Version,
                registry.ToDomainObject<IDomainEvent>(
                    x.Body ?? throw new InvalidOperationException($"Stored event '{x.Id}' is missing a body."))))
            .ToArray();

        return events;
    }
}
