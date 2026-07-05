using Raven.Client.Documents.Session;
using Soundtrail.Contracts.EventSourcing;
using Soundtrail.Domain.Catalog;
using Soundtrail.Tools.MusicBrainzImport.Features.ReplayCatalogProjection.EventStore;

namespace Soundtrail.Tools.MusicBrainzImport.Features.ReplayCatalogProjection.Adapters;

public sealed class RavenLoadCatalogProjectionReplayTargets(
    IAsyncDocumentSession session) : ILoadCatalogProjectionReplayTargetsPort
{
    public async Task<IReadOnlyList<ArtistId>> LoadAsync(CancellationToken cancellationToken)
    {
        var metadata = await session.Advanced.LoadStartingWithAsync<RavenEventStreamMetadataRecord>(
            "artist-catalog-streams/",
            start: 0,
            pageSize: 4096);

        return metadata
            .Select(x => ArtistId.From(x.StreamId))
            .Distinct()
            .OrderBy(x => x.Value, StringComparer.Ordinal)
            .ToArray();
    }
}
