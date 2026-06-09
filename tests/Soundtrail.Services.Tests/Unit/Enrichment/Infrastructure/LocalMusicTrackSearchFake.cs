using System.Collections.Concurrent;
using Soundtrail.Contracts.Common;
using Soundtrail.Services.Enrichment.DiscoveryPlanner.Features.JustInTimeScheduling.LocalSearch;

namespace Soundtrail.Services.Tests.Unit.Enrichment.Infrastructure;

public sealed class LocalMusicTrackSearchFake : ILocalMusicTrackSearch
{
    private readonly ConcurrentDictionary<string, LocalMusicTrackSearchResult> results = [];

    public Task<LocalMusicTrackSearchResult?> GetByMusicCatalogIdAsync(
        MusicCatalogId musicCatalogId,
        CancellationToken cancellationToken)
    {
        results.TryGetValue(musicCatalogId.Value, out var result);
        return Task.FromResult(result);
    }

    public void Seed(LocalMusicTrackSearchResult result) => results[result.MusicCatalogId.Value] = result;
}
