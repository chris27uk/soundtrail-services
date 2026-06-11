using Soundtrail.Contracts.Commands;
using Soundtrail.Contracts.Common;
using Soundtrail.Services.Enrichment.DiscoveryPlanner.Features.JustInTimeScheduling.Adapters;
using Soundtrail.Services.Enrichment.DiscoveryPlanner.Features.JustInTimeScheduling;
using Soundtrail.Services.Enrichment.DiscoveryPlanner.Features.JustInTimeScheduling.LocalSearch;
using Soundtrail.Services.Enrichment.DiscoveryPlanner.Shared.Prioritisation;
using Soundtrail.Services.Enrichment.DiscoveryPlanner.Shared.Search.Resolution;

namespace Soundtrail.Services.Tests.Unit.Enrichment.Infrastructure;

internal sealed class LookupMusicRequestListenerTestEnvironment
{
    private static readonly DateTimeOffset DefaultOccurredAt = new(2026, 6, 8, 12, 0, 0, TimeSpan.Zero);
    private readonly LocalMusicTrackSearchFake localSearchFake;

    private LookupMusicRequestListenerTestEnvironment(FakeMusicCatalogCandidateSearch search)
    {
        localSearchFake = new LocalMusicTrackSearchFake();
        localSearchFake.Seed(new LocalMusicTrackSearchResult(
            MusicCatalogId.From("mc_track_1"),
            "Song A",
            "Artist A",
            "Album A",
            null,
            null,
            null,
            IsPlayable: false));
        Listener = new LookupMusicRequestListener(
            new LookupMusicRequestHandler(
                search,
                new RankedMusicCandidateStoreFake(),
                new DiscoveryPriorityPolicy(),
                new MusicCatalogResolutionPolicy(),
                new ActiveLookupWorkStoreFake(),
                localSearchFake));
    }

    public LookupMusicRequestListener Listener { get; }

    public LocalMusicTrackSearchFake LocalSearch => localSearchFake;

    public static LookupMusicRequestListenerTestEnvironment WithASchedulableRequest()
    {
        var search = new FakeMusicCatalogCandidateSearch();
        search.ResolveAs(MusicCatalogId.From("mc_track_1"));
        return new LookupMusicRequestListenerTestEnvironment(search);
    }

    public static LookupMusicRequestListenerTestEnvironment WithAnUnschedulableRequest() =>
        new(new FakeMusicCatalogCandidateSearch());

    public Task<object[]> HandleSchedulableRequest() =>
        Listener.Handle(new LookupMusicRequestDto("rare unknown song", 1, 10, DefaultOccurredAt, "corr-1"), null!);

    public Task<object[]> HandleUnschedulableRequest() =>
        Listener.Handle(new LookupMusicRequestDto("rare unknown song", 0, 100, DefaultOccurredAt, "corr-1"), null!);
}
