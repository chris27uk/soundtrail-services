using Soundtrail.Contracts.Common;
using Soundtrail.Domain.Catalog.Projection;

namespace Soundtrail.Tools.MusicBrainzImport.Features.ReplayCatalogProjection.EventStore;

public interface ILoadMusicTrackEventsForCatalogReplayPort
{
    Task<IReadOnlyList<VersionedMusicTrackEvent>> LoadAsync(
        MusicCatalogId musicCatalogId,
        CancellationToken cancellationToken);
}
