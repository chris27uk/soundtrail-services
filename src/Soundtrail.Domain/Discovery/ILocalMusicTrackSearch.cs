using Soundtrail.Contracts.Common;

namespace Soundtrail.Domain.Discovery;

public interface ILocalMusicTrackSearch
{
    Task<LocalMusicTrackSearchResult?> GetByMusicCatalogIdAsync(
        MusicCatalogId musicCatalogId,
        CancellationToken cancellationToken);
}
