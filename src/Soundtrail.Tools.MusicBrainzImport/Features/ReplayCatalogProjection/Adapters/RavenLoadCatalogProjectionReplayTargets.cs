using Raven.Client.Documents.Session;
using Soundtrail.Contracts.Common;
using Soundtrail.Contracts.EventSourcing;
using Soundtrail.Tools.MusicBrainzImport.Features.ReplayCatalogProjection.EventStore;

namespace Soundtrail.Tools.MusicBrainzImport.Features.ReplayCatalogProjection.Adapters;

public sealed class RavenLoadCatalogProjectionReplayTargets(
    IAsyncDocumentSession session) : ILoadCatalogProjectionReplayTargetsPort
{
    public async Task<IReadOnlyList<MusicCatalogId>> LoadAsync(CancellationToken cancellationToken)
    {
        var metadata = await session.Advanced.LoadStartingWithAsync<MusicTrackEventStreamMetadataRecordDto>(
            "music-track-streams/",
            start: 0,
            pageSize: 4096);

        return metadata
            .Select(x => MusicCatalogId.From(x.MusicCatalogId))
            .Distinct()
            .OrderBy(x => x.Value, StringComparer.Ordinal)
            .ToArray();
    }
}
