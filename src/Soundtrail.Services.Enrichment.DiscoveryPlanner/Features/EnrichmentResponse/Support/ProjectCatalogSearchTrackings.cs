using Soundtrail.Domain.Discovery;

namespace Soundtrail.Services.Enrichment.DiscoveryPlanner.Features.EnrichmentResponse.Support;

public sealed class ProjectCatalogSearchTrackings(ICatalogSearchTrackingStore catalogSearchTrackingStore)
{
    public async Task ProjectAsync(Domain.Responses.EnrichmentResponse response, CancellationToken cancellationToken)
    {
        foreach (var criteria in CatalogSearchCriteriaSet.ForResolvedTrack(
                     response.MusicCatalogId,
                     response.Hierarchy?.ArtistId,
                     response.Hierarchy?.AlbumId))
        {
            await catalogSearchTrackingStore.UpsertAsync(
                new CatalogSearchTracking(
                    criteria,
                    response.MusicCatalogId,
                    response.CreatedAt),
                cancellationToken);
        }
    }
}
