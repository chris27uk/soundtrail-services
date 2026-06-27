using Soundtrail.Contracts.Common;
using Soundtrail.Domain.Catalog.Events;

namespace Soundtrail.Domain.Catalog.Projection;

public interface IMusicTrackEventRepository
{
    Task<MusicTrackStream> LoadEventsAsync(
        MusicCatalogId musicCatalogId,
        CancellationToken cancellationToken);

    Task<AppendMusicTrackStreamResult> AppendEventsAsync(
        MusicCatalogId musicCatalogId,
        int expectedVersion,
        CommandId commandId,
        IReadOnlyList<IMusicTrackEvent> events,
        CancellationToken cancellationToken);
}
