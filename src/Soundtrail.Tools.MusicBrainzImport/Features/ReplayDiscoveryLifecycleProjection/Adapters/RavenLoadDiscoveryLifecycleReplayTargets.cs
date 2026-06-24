using Raven.Client.Documents.Session;
using Soundtrail.Contracts.EventSourcing;
using Soundtrail.Domain.Discovery;

namespace Soundtrail.Tools.MusicBrainzImport.Features.OnReplayCatalogSearchStatus.Adapters;

public sealed class RavenLoadDiscoveryLifecycleReplayTargets(
    IAsyncDocumentSession session) : ILoadDiscoveryLifecycleReplayTargetsPort
{
    public async Task<IReadOnlyList<CatalogSearchCriteria>> LoadAsync(CancellationToken cancellationToken)
    {
        var metadata = await session.Advanced.LoadStartingWithAsync<DiscoveryQueryEventStreamMetadataRecordDto>(
            "discovery-query-streams/",
            start: 0,
            pageSize: 4096);

        return metadata
            .Select(x => CatalogSearchCriteria.From(x.Criteria))
            .Distinct()
            .OrderBy(x => x.Value, StringComparer.Ordinal)
            .ToArray();
    }
}
