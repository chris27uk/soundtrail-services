using Soundtrail.Services.Features.Search;
using Soundtrail.Services.Features.Search.Contracts;
using Soundtrail.Services.Features.Search.Models;
using Soundtrail.Services.Features.Search.Queueing;
using Soundtrail.Services.Features.Tracks;

namespace Soundtrail.Services.Tests.Api.Unit.Features.Search;

internal sealed class SearchMusicTestEnvironment
{
    private SearchMusicTestEnvironment(
        FakeTrackSearchPort trackSearch,
        FakeEnqueueMusicRequest enqueueMusicRequests)
    {
        TrackSearch = trackSearch;
        EnqueueMusicRequests = enqueueMusicRequests;
    }

    public FakeTrackSearchPort TrackSearch { get; }

    public FakeEnqueueMusicRequest EnqueueMusicRequests { get; }

    public static SearchMusicTestEnvironment WithCachedResolvedResponse()
    {
        return new SearchMusicTestEnvironment(new FakeTrackSearchPort(), new FakeEnqueueMusicRequest());
    }

    public static SearchMusicTestEnvironment WithKnownTrack() =>
        new(
            new FakeTrackSearchPort(KnownTracks.MrBrightside()),
            new FakeEnqueueMusicRequest());

    public static SearchMusicTestEnvironment WithCachedAndKnownTracks(params SearchResult[] results)
    {
        return new SearchMusicTestEnvironment(
            new FakeTrackSearchPort(results),
            new FakeEnqueueMusicRequest());
    }

    public static SearchMusicTestEnvironment WithNoKnownTracks() =>
        new(new FakeTrackSearchPort(), new FakeEnqueueMusicRequest());

    public SearchMusicHandler CreateHandler() => new(TrackSearch, EnqueueMusicRequests);

    public SearchMusicRequest SearchForKnownTrack() =>
        new(SearchQuery.From("mr brightside"), Limit.From(10));

    public SearchMusicRequest SearchForUnknownTrack() =>
        new(SearchQuery.From("rare unknown song"), Limit.From(10));
}

internal sealed class FakeTrackSearchPort : ITrackSearchPort
{
    private readonly IReadOnlyList<SearchResult> _results;

    public FakeTrackSearchPort(params SearchResult[] results)
    {
        this._results = results;
    }

    public Task<IReadOnlyList<SearchResult>> SearchAsync(
        NormalizedSearchQuery query,
        Limit limit,
        CancellationToken cancellationToken)
    {
        var results = this._results
            .Where(track => NormalizedSearchQuery.FromText($"{track.Title.Value} {track.Artist.Value}")
                .Value.Contains(query.Value, StringComparison.Ordinal))
            .Take(limit.Value)
            .ToArray();

        return Task.FromResult<IReadOnlyList<SearchResult>>(results);
    }

    public Task<bool> IsReadyAsync(CancellationToken cancellationToken) => Task.FromResult(true);
}

internal sealed class FakeEnqueueMusicRequest : IEnqueueMusicRequest
{
    private readonly List<LookupMusicRequest> requests = [];

    public IReadOnlyList<LookupMusicRequest> Requests => this.requests;

    public Task EnqueueAsync(
        LookupMusicRequest request,
        CancellationToken cancellationToken)
    {
        this.requests.Add(request);
        return Task.CompletedTask;
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
