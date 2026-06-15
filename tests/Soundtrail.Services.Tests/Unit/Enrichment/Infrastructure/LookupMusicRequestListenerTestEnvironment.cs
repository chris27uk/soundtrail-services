using Soundtrail.Contracts.Common;
using Soundtrail.Contracts.IntegrationMessaging.Commands;
using Soundtrail.Domain.Discovery;
using Soundtrail.Services.Enrichment.DiscoveryPlanner.Features.JustInTimeScheduling.Adapters;
using Soundtrail.Services.Enrichment.DiscoveryPlanner.Features.JustInTimeScheduling;
using Soundtrail.Services.Enrichment.DiscoveryPlanner.Shared.Prioritisation;
using Soundtrail.Services.Enrichment.DiscoveryPlanner.Shared.Search.Resolution;

namespace Soundtrail.Services.Tests.Unit.Enrichment.Infrastructure;

internal sealed class LookupMusicRequestListenerTestEnvironment
{
    private static readonly DateTimeOffset DefaultOccurredAt = new(2026, 6, 8, 12, 0, 0, TimeSpan.Zero);
    private readonly LocalMusicTrackSearchFake localSearchFake;
    private readonly InMemoryUpsertDiscoveryStatus discoveryStatus;

    private LookupMusicRequestListenerTestEnvironment(FakeMusicCatalogCandidateSearch search)
    {
        localSearchFake = new LocalMusicTrackSearchFake();
        discoveryStatus = new InMemoryUpsertDiscoveryStatus();
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
                new PotentialCatalogLookupWorkStoreFake(),
                new DiscoveryPriorityPolicy(),
                new MusicCatalogMatchResolver(),
                new ActiveLookupWorkStoreFake(),
                localSearchFake),
            discoveryStatus);
    }

    public LookupMusicRequestListener Listener { get; }

    public LocalMusicTrackSearchFake LocalSearch => localSearchFake;

    public InMemoryUpsertDiscoveryStatus DiscoveryStatus => discoveryStatus;

    public static LookupMusicRequestListenerTestEnvironment WithASchedulableRequest()
    {
        var search = new FakeMusicCatalogCandidateSearch();
        search.ResolveAs(MusicCatalogId.From("mc_track_1"));
        return new LookupMusicRequestListenerTestEnvironment(search);
    }

    public static LookupMusicRequestListenerTestEnvironment WithAnUnschedulableRequest()
    {
        var search = new FakeMusicCatalogCandidateSearch();
        search.Fails();
        return new LookupMusicRequestListenerTestEnvironment(search);
    }

    public static LookupMusicRequestListenerTestEnvironment WithADeferredRequest()
    {
        var search = new FakeMusicCatalogCandidateSearch();
        search.ResolveAs(MusicCatalogId.From("mc_track_1"));
        return new LookupMusicRequestListenerTestEnvironment(search);
    }

    public Task<object[]> HandleSchedulableRequest() =>
        Listener.Handle(new LookupMusicRequestDto("search:track:rare unknown song", "rare unknown song", 1, 10, DefaultOccurredAt, "corr-1"), null!);

    public Task<object[]> HandleUnschedulableRequest() =>
        Listener.Handle(new LookupMusicRequestDto("search:track:rare unknown song", "rare unknown song", 0, 100, DefaultOccurredAt, "corr-1"), null!);

    public Task<object[]> HandleDeferredRequest() =>
        Listener.Handle(new LookupMusicRequestDto("search:track:rare unknown song", "rare unknown song", 0, 100, DefaultOccurredAt, "corr-1"), null!);
}
