using Soundtrail.Services.Enrichment.Shared.Search;
using Soundtrail.Services.Shared;

namespace Soundtrail.Services.Enrichment.Shared.MusicTracks;

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
