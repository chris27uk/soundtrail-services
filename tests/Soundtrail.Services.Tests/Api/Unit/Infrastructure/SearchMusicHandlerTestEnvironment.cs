using Soundtrail.Services.Features.Search;
using Soundtrail.Services.Features.Search.TrackSearch;
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