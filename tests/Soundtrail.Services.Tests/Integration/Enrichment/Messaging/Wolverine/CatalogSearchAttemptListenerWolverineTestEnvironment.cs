using Soundtrail.Contracts.Common;
using Soundtrail.Contracts.IntegrationMessaging.Commands;
using Soundtrail.Services.Enrichment.Orchestrator.Shared.Search;
using Soundtrail.Domain.Discovery;
using Soundtrail.Domain.Events;
using Soundtrail.Services.Enrichment.Orchestrator.Features.OnCatalogSearchRequested;
using Soundtrail.Services.Enrichment.Orchestrator.Features.OnCatalogSearchRequested.Adapters;
using Soundtrail.Services.Enrichment.Orchestrator.Infrastructure.Messaging;
using Soundtrail.Services.Enrichment.Orchestrator.Shared.Prioritisation;
using Soundtrail.Services.Enrichment.Orchestrator.Shared.Search.Resolution;
using Soundtrail.Services.Tests.Unit.Enrichment.Infrastructure;

namespace Soundtrail.Services.Tests.Integration.Enrichment.Messaging.Wolverine;

internal sealed class CatalogSearchRequestedListenerWolverineTestEnvironment
{
    private static readonly DateTimeOffset DefaultOccurredAt = new(2026, 6, 8, 12, 0, 0, TimeSpan.Zero);
    private readonly LocalMusicTrackSearchFake localSearchFake;
    private readonly CatalogSearchDiscoveryRepositoryFake discoveryRepository;
    private readonly SourceApiBudgetPortFake sourceBudgetFake;
    private readonly WolverineMessageBusFake messageBusFake;

    private CatalogSearchRequestedListenerWolverineTestEnvironment(FakeMusicCatalogCandidateSearch search)
    {
        localSearchFake = new LocalMusicTrackSearchFake();
        discoveryRepository = new CatalogSearchDiscoveryRepositoryFake();
        sourceBudgetFake = new SourceApiBudgetPortFake();
        messageBusFake = new WolverineMessageBusFake();
        localSearchFake.Seed(new LocalMusicTrackSearchResult(
            MusicCatalogId.From("mc_track_1"),
            "Song A",
            "Artist A",
            "Album A",
            null,
            null,
            null,
            IsPlayable: false));
        Listener = new CatalogSearchRequestedListener(
            new CatalogSearchRequestedHandler(
                search,
                new PotentialCatalogLookupWorkStoreFake(),
                new CatalogSearchTrackingStoreFake(),
                discoveryRepository,
                new DiscoveryPriorityPolicy(),
                sourceBudgetFake,
                new MusicCatalogMatchResolver(),
                new ActiveLookupWorkStoreFake(),
                localSearchFake,
                new WolverineCommandBus(messageBusFake)));
    }

    public CatalogSearchRequestedListener Listener { get; }

    public LocalMusicTrackSearchFake LocalSearch => localSearchFake;

    public CatalogSearchDiscoveryRepositoryFake DiscoveryRepository => discoveryRepository;

    public IReadOnlyList<object> SentMessages => messageBusFake.SentMessages;

    public static CatalogSearchRequestedListenerWolverineTestEnvironment WithASchedulableRequest()
    {
        var search = new FakeMusicCatalogCandidateSearch();
        search.ResolveAs(MusicCatalogId.From("mc_track_1"));
        return new CatalogSearchRequestedListenerWolverineTestEnvironment(search);
    }

    public static CatalogSearchRequestedListenerWolverineTestEnvironment WithAnUnschedulableRequest()
    {
        var search = new FakeMusicCatalogCandidateSearch();
        search.Fails();
        return new CatalogSearchRequestedListenerWolverineTestEnvironment(search);
    }

    public static CatalogSearchRequestedListenerWolverineTestEnvironment WithADeferredRequest()
    {
        var search = new FakeMusicCatalogCandidateSearch();
        search.ResolveAs(MusicCatalogId.From("mc_track_1"));
        return new CatalogSearchRequestedListenerWolverineTestEnvironment(search);
    }

    public async Task<IReadOnlyList<object>> HandleSchedulableRequest()
    {
        await Listener.Handle(new CatalogSearchAttemptDto("search:track:rare unknown song", "rare unknown song", 1, 10, DefaultOccurredAt, "corr-1"), null!);
        return SentMessages;
    }

    public async Task<IReadOnlyList<object>> HandleUnschedulableRequest()
    {
        await Listener.Handle(new CatalogSearchAttemptDto("search:track:rare unknown song", "rare unknown song", 0, 100, DefaultOccurredAt, "corr-1"), null!);
        return SentMessages;
    }

    public async Task<IReadOnlyList<object>> HandleDeferredRequest()
    {
        await Listener.Handle(new CatalogSearchAttemptDto("search:track:rare unknown song", "rare unknown song", 0, 100, DefaultOccurredAt, "corr-1"), null!);
        return SentMessages;
    }
}
