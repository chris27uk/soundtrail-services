using Soundtrail.Contracts.Common;
using Soundtrail.Domain.Model;

namespace Soundtrail.Tools.MusicBrainzImport.Features.ReplayCatalogProjection;

public interface ILoadMusicTrackEventsForCatalogReplayPort
{
    Task<IReadOnlyList<VersionedMusicTrackEvent>> LoadAsync(
        MusicCatalogId musicCatalogId,
        CancellationToken cancellationToken);
}
