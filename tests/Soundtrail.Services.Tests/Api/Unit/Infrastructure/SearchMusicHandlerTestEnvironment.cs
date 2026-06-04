using Soundtrail.Services.Features.Search;
using Soundtrail.Services.Features.Search.Contracts;
using Soundtrail.Services.Features.Search.Models;
using Soundtrail.Services.Features.Search.Queueing;
using Soundtrail.Services.Features.Tracks;
using Soundtrail.Services.Shared;

namespace Soundtrail.Services.Tests.Api.Unit.Infrastructure;

internal sealed class SearchMusicHandlerTestEnvironment
{
    private SearchMusicHandlerTestEnvironment(
        FakeTrackSearchPort trackSearch,
        FakeEnqueueMusicRequest enqueueMusicRequests)
    {
        TrackSearch = trackSearch;
        EnqueueMusicRequests = enqueueMusicRequests;
        Handler = new SearchMusicHandler(trackSearch, enqueueMusicRequests);
    }

    public IHandler<SearchMusicRequest, SearchMusicResponse> Handler { get; }

    public FakeTrackSearchPort TrackSearch { get; }

    public FakeEnqueueMusicRequest EnqueueMusicRequests { get; }

    public static SearchMusicHandlerTestEnvironment WithKnownTrack() =>
        new(
            new FakeTrackSearchPort(KnownSearchResults.MrBrightside()),
            new FakeEnqueueMusicRequest());

    public static SearchMusicHandlerTestEnvironment WithMultipleKnownTracks() =>
        new(
            new FakeTrackSearchPort(
                KnownSearchResults.MrBrightside(),
                KnownSearchResults.WhenYouWereYoung()),
            new FakeEnqueueMusicRequest());

    public static SearchMusicHandlerTestEnvironment WithNoKnownTracks() =>
        new(new FakeTrackSearchPort(), new FakeEnqueueMusicRequest());

    public SearchMusicRequest Request(
        string query,
        double? minConfidence = null,
        int? limit = null) =>
        new(
            SearchQuery.From(query),
            Limit.From(limit),
            minConfidence is null ? null : ConfidenceScore.From(minConfidence.Value));
}

internal sealed class FakeTrackSearchPort(params SearchResult[] results) : ITrackSearchPort
{
    private readonly IReadOnlyList<SearchResult> searchResults = results;

    public Task<IReadOnlyList<SearchResult>> SearchAsync(
        NormalizedSearchQuery query,
        Limit limit,
        CancellationToken cancellationToken)
    {
        var matches = searchResults
            .Where(track => NormalizedSearchQuery.FromText($"{track.Title.Value} {track.Artist.Value}")
                .Value.Contains(query.Value, StringComparison.Ordinal))
            .Take(limit.Value)
            .ToArray();

        return Task.FromResult<IReadOnlyList<SearchResult>>(matches);
    }

    public Task<bool> IsReadyAsync(CancellationToken cancellationToken) => Task.FromResult(true);
}

internal sealed class FakeEnqueueMusicRequest : IEnqueueMusicRequest
{
    private readonly List<LookupMusicRequest> requests = [];

    public IReadOnlyList<LookupMusicRequest> Requests => requests;

    public Task EnqueueAsync(
        LookupMusicRequest request,
        CancellationToken cancellationToken)
    {
        requests.Add(request);
        return Task.CompletedTask;
    }
}

internal static class KnownSearchResults
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

    public static SearchResult WhenYouWereYoung() =>
        new(
            TrackTitle.From("When You Were Young"),
            ArtistName.From("The Killers"),
            Isrc.From("USIR20600065"),
            Mbid.From("when-you-were-young-mbid"),
            AppleId.From("apple-when-you-were-young"),
            SpotifyId.From("spotify-when-you-were-young"),
            ConfidenceScore.From(0.95));
}
