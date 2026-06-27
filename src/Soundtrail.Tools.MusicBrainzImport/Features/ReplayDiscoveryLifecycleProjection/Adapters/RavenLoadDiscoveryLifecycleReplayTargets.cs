using Raven.Client.Documents.Session;
using Soundtrail.Contracts.EventSourcing;
using Soundtrail.Domain.Discovery;
using Soundtrail.Domain.Model;
using Soundtrail.Translators.Discovery;

namespace Soundtrail.Tools.MusicBrainzImport.Features.OnReplayCatalogSearchStatus.Adapters;

public sealed class RavenLoadDiscoveryLifecycleReplayTargets(
    IAsyncDocumentSession session) : ILoadDiscoveryLifecycleReplayTargetsPort
{
    public async Task<IReadOnlyList<MusicSearchCriteria>> LoadAsync(CancellationToken cancellationToken)
    {
        var metadata = await session.Advanced.LoadStartingWithAsync<DiscoveryQueryEventStreamMetadataRecordDto>(
            "discovery-query-streams/",
            start: 0,
            pageSize: 4096);

        return metadata
            .Select(x => MusicSearchTermPersistentIdTranslator.ToDomainObject(x.Criteria))
            .Distinct()
            .OrderBy(MusicSearchTermPersistentIdTranslator.ToPersistentId, StringComparer.Ordinal)
            .ToArray();
    }
}
