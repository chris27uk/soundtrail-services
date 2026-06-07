using Soundtrail.Services.Enrichment.Shared.Search;

namespace Soundtrail.Services.Enrichment.Shared.MusicTracks;

public interface IMusicTrackProjectionStore
{
    Task StoreAsync(
        MusicCatalogId musicCatalogId,
        MusicTrack musicTrack,
        CancellationToken cancellationToken);
}
