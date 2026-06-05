using Soundtrail.Services.Enrichment.Shared.Execution;
using Soundtrail.Services.Enrichment.Shared.Search;

namespace Soundtrail.Services.Tests.Enrichment.Unit.Infrastructure;

public sealed class TrackEnrichmentWriteStoreFake : ITrackEnrichmentWriteStore
{
    private readonly Dictionary<string, TrackEnrichmentState> byMusicCatalogId = [];

    public IReadOnlyDictionary<string, TrackEnrichmentState> States => byMusicCatalogId;

    public Task ApplyAsync(
        MusicCatalogId musicCatalogId,
        Action<TrackEnrichmentState> apply,
        CancellationToken cancellationToken)
    {
        if (!byMusicCatalogId.TryGetValue(musicCatalogId.Value, out var state))
        {
            state = new TrackEnrichmentState();
            byMusicCatalogId[musicCatalogId.Value] = state;
        }

        apply(state);
        return Task.CompletedTask;
    }
}
