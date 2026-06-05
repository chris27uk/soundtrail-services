using Soundtrail.Services.Enrichment.Shared.Search;

namespace Soundtrail.Services.Enrichment.Shared.Execution;

public interface ITrackEnrichmentWriteStore
{
    Task ApplyAsync(
        MusicCatalogId musicCatalogId,
        Action<TrackEnrichmentState> apply,
        CancellationToken cancellationToken);
}
