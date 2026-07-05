using Raven.Client.Documents.Session;
using Soundtrail.Contracts.EventSourcing;
using Soundtrail.Domain.Search;
using Soundtrail.Tools.MusicBrainzImport.Features.ReplayDiscoveryLifecycleProjection.EventStore;

namespace Soundtrail.Tools.MusicBrainzImport.Features.ReplayDiscoveryLifecycleProjection.Adapters;

public sealed class RavenLoadDiscoveryLifecycleReplayTargets(
    IAsyncDocumentSession session) : ILoadDiscoveryLifecycleReplayTargetsPort
{
    public async Task<IReadOnlyList<LookupCriteria>> LoadAsync(CancellationToken cancellationToken)
    {
        var metadata = await session.Advanced.LoadStartingWithAsync<RavenEventStreamMetadataRecord>(
            "discovery-query-streams/",
            start: 0,
            pageSize: 4096);

        return metadata
            .Select(x => DiscoveryQueryKey.ToMusicSearchCriteria(x.StreamId))
            .Distinct()
            .OrderBy(DiscoveryQueryKey.StableValueFor, StringComparer.Ordinal)
            .ToArray();
    }
}
