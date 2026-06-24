using Soundtrail.Contracts.Common;

namespace Soundtrail.Services.Enrichment.Orchestrator.Shared.Search;

public interface ILocalMusicTrackSearch
{
    Task<LocalMusicTrackSearchResult?> GetByMusicCatalogIdAsync(
        MusicCatalogId musicCatalogId,
        CancellationToken cancellationToken);
}
