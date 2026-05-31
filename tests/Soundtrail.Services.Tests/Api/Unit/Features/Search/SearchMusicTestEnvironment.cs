using Soundtrail.Services.Features.Search.Contracts;
using Soundtrail.Services.Features.Search;
using Soundtrail.Services.Features.Search.Models;
using Soundtrail.Services.Features.Tracks;

namespace Soundtrail.Services.Tests.Unit.Features.Search;

internal sealed class SearchMusicTestEnvironment
{
    private SearchMusicTestEnvironment(
        FakeQueryCachePort queryCache,
        FakeTrackSearchPort trackSearch,
        FakeResolutionDemandPort demandStore,
        FakeResolutionDemandSignalPort demandSignals)
    {
        QueryCache = queryCache;
        TrackSearch = trackSearch;
        DemandStore = demandStore;
        DemandSignals = demandSignals;
    }

    public FakeQueryCachePort QueryCache { get; }

    public FakeTrackSearchPort TrackSearch { get; }

    public FakeResolutionDemandPort DemandStore { get; }

    public FakeResolutionDemandSignalPort DemandSignals { get; }

    public static SearchMusicTestEnvironment WithCachedResolvedResponse()
    {
        var queryCache = new FakeQueryCachePort();
        var knownTrack = KnownTracks.MrBrightside();
        var query = SearchQuery.From("mr brightside");

        queryCache.StoreAsync(
            NormalizedSearchQuery.From(query),
            SearchMusicResponse.Resolved(query, new[] { knownTrack }, "cache"),
            TimeSpan.FromHours(1),
            CancellationToken.None).GetAwaiter().GetResult();

        return new SearchMusicTestEnvironment(queryCache, new FakeTrackSearchPort(), new FakeResolutionDemandPort(), new FakeResolutionDemandSignalPort());
    }

    public static SearchMusicTestEnvironment WithKnownTrack() =>
        new(
            new FakeQueryCachePort(),
            new FakeTrackSearchPort(KnownTracks.MrBrightside()),
            new FakeResolutionDemandPort(),
            new FakeResolutionDemandSignalPort());

    public static SearchMusicTestEnvironment WithCachedAndKnownTracks(params SearchResult[] results)
    {
        var queryCache = new FakeQueryCachePort();
        var query = SearchQuery.From("mr brightside");

        queryCache.StoreAsync(
            NormalizedSearchQuery.From(query),
            SearchMusicResponse.Resolved(query, new[] { KnownTracks.LowConfidenceMrBrightside() }, "cache"),
            TimeSpan.FromHours(1),
            CancellationToken.None).GetAwaiter().GetResult();

        queryCache.ResetCounters();

        return new SearchMusicTestEnvironment(
            queryCache,
            new FakeTrackSearchPort(results),
            new FakeResolutionDemandPort(),
            new FakeResolutionDemandSignalPort());
    }

    public static SearchMusicTestEnvironment WithNoKnownTracks() =>
        new(
            new FakeQueryCachePort(),
            new FakeTrackSearchPort(),
            new FakeResolutionDemandPort(),
            new FakeResolutionDemandSignalPort());

    public SearchMusicHandler CreateHandler() => new(QueryCache, TrackSearch, DemandStore, DemandSignals);

    public SearchMusicRequest SearchForKnownTrack() =>
        new(SearchQuery.From("mr brightside"), Limit.From(10));

    public SearchMusicRequest SearchForUnknownTrack() =>
        new(SearchQuery.From("rare unknown song"), Limit.From(10));
}

internal sealed class FakeQueryCachePort : IQueryCachePort
{
    private readonly Dictionary<string, SearchMusicResponse> _responses = new();

    public int GetCallCount { get; private set; }

    public int StoreCallCount { get; private set; }

    public Task<SearchMusicResponse?> GetAsync(NormalizedSearchQuery query, CancellationToken cancellationToken)
    {
        GetCallCount++;
        _responses.TryGetValue(query.Value, out var response);
        return Task.FromResult(response);
    }

    public Task StoreAsync(
        NormalizedSearchQuery query,
        SearchMusicResponse response,
        TimeSpan timeToLive,
        CancellationToken cancellationToken)
    {
        StoreCallCount++;
        _responses[query.Value] = response;
        return Task.CompletedTask;
    }

    public Task<bool> IsReadyAsync(CancellationToken cancellationToken) => Task.FromResult(true);

    public void ResetCounters()
    {
        GetCallCount = 0;
        StoreCallCount = 0;
    }
}

internal sealed class FakeTrackSearchPort : ITrackSearchPort
{
    private readonly IReadOnlyList<SearchResult> _results;

    public FakeTrackSearchPort(params SearchResult[] results)
    {
        _results = results;
    }

    public Task<IReadOnlyList<SearchResult>> SearchAsync(
        NormalizedSearchQuery query,
        Limit limit,
        CancellationToken cancellationToken)
    {
        var results = _results
            .Where(track => NormalizedSearchQuery.FromText($"{track.Title.Value} {track.Artist.Value}")
                .Value.Contains(query.Value, StringComparison.Ordinal))
            .Take(limit.Value)
            .ToArray();

        return Task.FromResult<IReadOnlyList<SearchResult>>(results);
    }

    public Task<bool> IsReadyAsync(CancellationToken cancellationToken) => Task.FromResult(true);
}

internal sealed class FakeResolutionDemandPort : IResolutionDemandPort
{
    private readonly Dictionary<string, QueryId> _queries = new();

    public IReadOnlyCollection<string> RecordedQueries => _queries.Keys.ToArray();

    public Task<QueryId> RecordDemandAsync(
        NormalizedSearchQuery query,
        CancellationToken cancellationToken)
    {
        if (!_queries.TryGetValue(query.Value, out var queryId))
        {
            queryId = QueryId.New();
            _queries.Add(query.Value, queryId);
        }

        return Task.FromResult(queryId);
    }
}

internal sealed class FakeResolutionDemandSignalPort : IResolutionDemandSignalPort
{
    private readonly List<ResolutionDemandSignal> signals = [];

    public IReadOnlyList<ResolutionDemandSignal> Signals => signals;

    public Task EnqueueAsync(
        ResolutionDemandSignal signal,
        CancellationToken cancellationToken)
    {
        signals.Add(signal);
        return Task.CompletedTask;
    }

    public ValueTask<ResolutionDemandSignal?> DequeueAsync(
        CancellationToken cancellationToken)
    {
        if (signals.Count == 0)
        {
            return ValueTask.FromResult<ResolutionDemandSignal?>(null);
        }

        var signal = signals[0];
        signals.RemoveAt(0);
        return ValueTask.FromResult<ResolutionDemandSignal?>(signal);
    }
}

internal static class KnownTracks
{
    public static SearchResult MrBrightside() =>
        new(
            TrackTitle.From("Mr. Brightside"),
            ArtistName.From("The Killers"),
            Isrc.From("USIR20400274"),
            Mbid.From("mr-brightside-mbid"),
            AppleId.From("apple-mr-brightside"),
            SpotifyId.From("spotify-mr-brightside"),
            ConfidenceScore.From(0.98));

    public static SearchResult LowConfidenceMrBrightside() =>
        new(
            TrackTitle.From("Mr. Brightside"),
            ArtistName.From("The Killers"),
            Isrc.From("USIR20400274"),
            Mbid.From("mr-brightside-mbid"),
            AppleId.From("apple-mr-brightside"),
            SpotifyId.From("spotify-mr-brightside"),
            ConfidenceScore.From(0.40));
}
