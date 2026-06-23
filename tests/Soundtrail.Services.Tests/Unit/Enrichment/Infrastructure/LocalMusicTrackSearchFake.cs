using System.Collections.Concurrent;
using Soundtrail.Contracts.Common;
using Soundtrail.Services.Enrichment.Orchestrator.Shared.Search;
using Soundtrail.Domain.Discovery;

namespace Soundtrail.Services.Tests.Unit.Enrichment.Infrastructure;

public sealed class LocalMusicTrackSearchFake : ILocalMusicTrackSearch
{
    private readonly ConcurrentDictionary<string, LocalMusicTrackSearchResult> results = [];

    public static LocalMusicTrackSearchFake CreateWith(params LocalMusicTrackSearchResult[] seededResults)
    {
        var fake = new LocalMusicTrackSearchFake();
        foreach (var result in seededResults)
        {
            fake.Seed(result);
        }

        return fake;
    }

    public static LocalMusicTrackSearchFake CreateForAsyncLookupHappyPath() =>
        CreateWith(
            new LocalMusicTrackSearchResult(
                MusicCatalogId.From("mc_track_1"),
                "Rare Unknown Song",
                "Test Artist",
                "Rare Album",
                Isrc: null,
                Mbid: null,
                DurationMs: null,
                IsPlayable: false,
                ReleaseDate: null));

    public Task<LocalMusicTrackSearchResult?> GetByMusicCatalogIdAsync(
        MusicCatalogId musicCatalogId,
        CancellationToken cancellationToken)
    {
        results.TryGetValue(musicCatalogId.Value, out var result);
        return Task.FromResult(result);
    }

    public void Seed(LocalMusicTrackSearchResult result) => results[result.MusicCatalogId.Value] = result;
}
