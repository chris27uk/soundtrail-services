using Soundtrail.Domain.Catalog;
using Soundtrail.Domain.Catalog.Projection;

namespace Soundtrail.Tools.MusicBrainzImport.Features.ReplayCatalogProjection.EventStore;

public interface ILoadMusicTrackEventsForCatalogReplayPort
{
    Task<IReadOnlyList<VersionedCatalogEvent>> LoadAsync(
        ArtistId artistId,
        CancellationToken cancellationToken);
}
