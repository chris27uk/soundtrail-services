using Raven.Client.Documents.Session;
using Soundtrail.Services.Enrichment.Shared.Execution;
using Soundtrail.Services.Enrichment.Shared.Search;
using Soundtrail.Services.Enrichment.Scheduler.Infrastructure.Raven.Documents;

namespace Soundtrail.Services.Enrichment.Scheduler.Infrastructure.Raven;

internal sealed class RavenTrackEnrichmentWriteStore(
    IAsyncDocumentSession session) : ITrackEnrichmentWriteStore
{
    public async Task ApplyAsync(
        MusicCatalogId musicCatalogId,
        Action<TrackEnrichmentState> apply,
        CancellationToken cancellationToken)
    {
        var documentId = RavenTrackDocument.GetDocumentId(musicCatalogId.Value);
        var document = await session.LoadAsync<RavenTrackDocument>(documentId, cancellationToken)
            ?? new RavenTrackDocument { Id = documentId };

        var state = document.ToDomain();
        apply(state);
        document.Apply(state);

        await session.StoreAsync(document, cancellationToken);
    }
}
