using Soundtrail.Services.Enrichment.DiscoveryPlanner.Shared.Search;

namespace Soundtrail.Services.Enrichment.DiscoveryPlanner.Shared.MusicTracks;

public interface IMusicTrackProjectionStore
{
    Task StoreAsync(
        MusicCatalogId musicCatalogId,
        MusicTrack musicTrack,
        CancellationToken cancellationToken);
}
