using Soundtrail.Contracts;
using Soundtrail.Domain;
using Soundtrail.Domain.Model;
using Soundtrail.Services.Api.Features.Search;
using Soundtrail.Services.Api.Features.Search.Queueing;
using Soundtrail.Services.Api.Features.Search.TrackSearch;

namespace Soundtrail.Services.Tests.Unit.Api.Infrastructure;

internal sealed class SearchMusicHandlerTestEnvironment
{
    private SearchMusicHandlerTestEnvironment(
        FakeTrackSearchPort trackSearch,
        InMemoryEnqueueMusicRequest enqueueMusicRequests)
    {
        TrackSearch = trackSearch;
        EnqueueMusicRequests = enqueueMusicRequests;
        Handler = new SearchMusicHandler(trackSearch, enqueueMusicRequests);
    }

    public IHandler<SearchMusicRequest, SearchMusicResponse> Handler { get; }

    public FakeTrackSearchPort TrackSearch { get; }

    public InMemoryEnqueueMusicRequest EnqueueMusicRequests { get; }

    public static SearchMusicHandlerTestEnvironment WithKnownTrack() =>
        new(
            new FakeTrackSearchPort(KnownSearchResults.MrBrightside()),
            new InMemoryEnqueueMusicRequest());

    public static SearchMusicHandlerTestEnvironment WithMultipleKnownTracks() =>
        new(
            new FakeTrackSearchPort(
                KnownSearchResults.MrBrightside(),
                KnownSearchResults.WhenYouWereYoung()),
            new InMemoryEnqueueMusicRequest());

    public static SearchMusicHandlerTestEnvironment WithNoKnownTracks() =>
        new(new FakeTrackSearchPort(), new InMemoryEnqueueMusicRequest());

    public SearchMusicRequest Request(
        string query,
        double? minConfidence = null,
        int? limit = null) =>
        new(
            NormalizedSearchQuery.FromText(query),
            Limit.From(limit),
            minConfidence is null ? null : ConfidenceScore.From(minConfidence.Value));
}
