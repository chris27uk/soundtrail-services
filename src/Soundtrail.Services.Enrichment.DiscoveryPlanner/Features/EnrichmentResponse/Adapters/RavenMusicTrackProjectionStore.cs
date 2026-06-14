using Raven.Client.Documents.Session;
using Soundtrail.Contracts.Common;
using Soundtrail.Domain.Model;

namespace Soundtrail.Services.Enrichment.DiscoveryPlanner.Features.EnrichmentResponse.Adapters;

public sealed class RavenMusicTrackProjectionStore(
    IAsyncDocumentSession session,
    MusicTrackProjectionApplier projectionApplier) : IMusicTrackProjectionStore
{
    public async Task StoreAsync(
        MusicCatalogId musicCatalogId,
        MusicTrackStream stream,
        CancellationToken cancellationToken)
    {
        await projectionApplier.ReplayStreamAsync(musicCatalogId, stream.Events, session, cancellationToken);
    }
}
