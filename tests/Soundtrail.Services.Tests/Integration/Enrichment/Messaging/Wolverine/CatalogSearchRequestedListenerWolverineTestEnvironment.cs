using Soundtrail.Contracts.Common;
using Soundtrail.Contracts.IntegrationMessaging.Commands;
using Soundtrail.Services.Enrichment.Orchestrator.Features.OnCatalogSearchRequested;
using Soundtrail.Services.Enrichment.Orchestrator.Features.OnCatalogSearchRequested.Adapters;
using Soundtrail.Services.Enrichment.Orchestrator.Shared.Search;
using Soundtrail.Services.Tests.Unit.Enrichment.Infrastructure;

namespace Soundtrail.Services.Tests.Integration.Enrichment.Messaging.Wolverine;

internal sealed class CatalogSearchRequestedListenerWolverineTestEnvironment
{
    private static readonly DateTimeOffset DefaultOccurredAt = new(2026, 6, 8, 12, 0, 0, TimeSpan.Zero);
    private readonly CatalogSearchDiscoveryRepositoryFake discoveryRepository;

    private CatalogSearchRequestedListenerWolverineTestEnvironment(FakeMusicCatalogCandidateSearch search)
    {
        discoveryRepository = new CatalogSearchDiscoveryRepositoryFake();

        Listener = new SearchCatalogRequestedListener(
            new SearchCatalogRequestedHandler(
                search,
                discoveryRepository));
    }

    public SearchCatalogRequestedListener Listener { get; }

    public CatalogSearchDiscoveryRepositoryFake DiscoveryRepository => discoveryRepository;

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

    public Task HandleSchedulableRequest() =>
        Listener.Handle(new CatalogSearchAttemptDto("search:track:rare unknown song", "rare unknown song", "spotify,appleMusic,youtubeMusic", 1, 10, DefaultOccurredAt, "corr-1"), null!);

    public Task HandleUnschedulableRequest() =>
        Listener.Handle(new CatalogSearchAttemptDto("search:track:rare unknown song", "rare unknown song", "spotify,appleMusic,youtubeMusic", 0, 100, DefaultOccurredAt, "corr-1"), null!);

    public Task HandleDeferredRequest() =>
        Listener.Handle(new CatalogSearchAttemptDto("search:track:rare unknown song", "rare unknown song", "spotify,appleMusic,youtubeMusic", 0, 100, DefaultOccurredAt, "corr-1"), null!);
}
