using Soundtrail.Contracts.Common;

using Soundtrail.Domain.Events;

namespace Soundtrail.Domain.Model;

public interface IMusicTrackEventRepository
{
    Task<MusicTrackStream> LoadEventsAsync(
        MusicCatalogId musicCatalogId,
        CancellationToken cancellationToken);

    Task<AppendMusicTrackStreamResult> AppendEventsAsync(
        MusicCatalogId musicCatalogId,
        int expectedVersion,
        CommandId commandId,
        IReadOnlyList<MusicTrackFact> events,
        CancellationToken cancellationToken);
}
