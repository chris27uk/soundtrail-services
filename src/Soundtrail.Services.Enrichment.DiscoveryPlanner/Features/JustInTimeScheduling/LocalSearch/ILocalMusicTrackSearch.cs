using Soundtrail.Contracts.Common;

namespace Soundtrail.Services.Enrichment.DiscoveryPlanner.Features.JustInTimeScheduling.LocalSearch;

public interface ILocalMusicTrackSearch
{
    Task<LocalMusicTrackSearchResult?> GetByMusicCatalogIdAsync(
        MusicCatalogId musicCatalogId,
        CancellationToken cancellationToken);
}
