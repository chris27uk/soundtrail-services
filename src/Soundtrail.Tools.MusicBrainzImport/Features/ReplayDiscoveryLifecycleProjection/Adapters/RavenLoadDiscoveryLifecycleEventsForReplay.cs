using Raven.Client.Documents.Session;
using Soundtrail.Adapters.Discovery;
using Soundtrail.Contracts.EventSourcing;
using Soundtrail.Domain.Discovery;
using Soundtrail.Domain.Search;
using Soundtrail.Services.Internal.Projector.Features.OnCatalogSearchStatusChanged.Adapters;
using Soundtrail.Tools.MusicBrainzImport.Features.ReplayDiscoveryLifecycleProjection.EventStore;

namespace Soundtrail.Tools.MusicBrainzImport.Features.ReplayDiscoveryLifecycleProjection.Adapters;

public sealed class RavenLoadDiscoveryLifecycleEventsForReplay(
    IAsyncDocumentSession session) : ILoadDiscoveryLifecycleEventsForReplayPort
{
    public async Task<IReadOnlyList<VersionedCatalogSearchDiscoveryEvent>> LoadAsync(
        MusicSearchCriteria searchCriteria,
        CancellationToken cancellationToken)
    {
        var persistentId = DiscoveryQueryKey.StableValueFor(searchCriteria);
        var events = (await session.Advanced.LoadStartingWithAsync<DiscoveryQueryStoredEventRecordDto>(
                $"discovery-query-events/{persistentId}/"))
            .OrderBy(x => x.Version)
            .Select(x => x.ToDomainEvent())
            .ToArray();

        return events;
    }
}
