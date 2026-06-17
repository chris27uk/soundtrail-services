using Soundtrail.Contracts.Common;

namespace Soundtrail.Domain.Model;

public interface IMusicTrackProjectionStore
{
    Task StoreAsync(
        MusicCatalogId musicCatalogId,
        MusicTrackStream stream,
        CancellationToken cancellationToken);
}
