using Soundtrail.Contracts.Common;
using Soundtrail.Contracts.IntegrationMessaging.Commands;
using Soundtrail.Services.Enrichment.Orchestrator.Features.OnCatalogSearchRequested;
using Soundtrail.Services.Enrichment.Orchestrator.Features.OnCatalogSearchRequested.Adapters;
using Soundtrail.Services.Enrichment.Orchestrator.Features.OnCatalogSearchRequested.Support;
using Soundtrail.Services.Enrichment.Orchestrator.Shared.Search;
using Soundtrail.Services.Tests.Unit.Enrichment.Infrastructure;

namespace Soundtrail.Services.Tests.Integration.Enrichment.Messaging.Wolverine;

internal sealed class CatalogSearchRequestedListenerWolverineTestEnvironment
{
    private static readonly DateTimeOffset DefaultOccurredAt = new(2026, 6, 8, 12, 0, 0, TimeSpan.Zero);
    private readonly LocalMusicTrackSearchFake localSearchFake;
    private readonly CatalogSearchDiscoveryRepositoryFake discoveryRepository;
    private readonly CommandBusFake commandBus;
    private readonly RecordCatalogSearchCandidateHandler recordCandidateHandler;

    private CatalogSearchRequestedListenerWolverineTestEnvironment(FakeMusicCatalogCandidateSearch search)
    {
        localSearchFake = new LocalMusicTrackSearchFake();
        discoveryRepository = new CatalogSearchDiscoveryRepositoryFake();
        commandBus = new CommandBusFake();
        recordCandidateHandler = new RecordCatalogSearchCandidateHandler(discoveryRepository);
        localSearchFake.Seed(new LocalMusicTrackSearchResult(
            MusicCatalogId.From("mc_track_1"),
            "Song A",
            "Artist A",
            "Album A",
            null,
            null,
            null,
            IsPlayable: false));

        Listener = new SearchCatalogRequestedListener(
            new SearchCatalogRequestedHandler(
                search,
                commandBus,
                localSearchFake));
    }

    public SearchCatalogRequestedListener Listener { get; }

    public LocalMusicTrackSearchFake LocalSearch => localSearchFake;

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
        HandleAndDrainAsync(new CatalogSearchAttemptDto("search:track:rare unknown song", "rare unknown song", "spotify,appleMusic,youtubeMusic", 1, 10, DefaultOccurredAt, "corr-1"));

    public Task HandleUnschedulableRequest() =>
        HandleAndDrainAsync(new CatalogSearchAttemptDto("search:track:rare unknown song", "rare unknown song", "spotify,appleMusic,youtubeMusic", 0, 100, DefaultOccurredAt, "corr-1"));

    public Task HandleDeferredRequest() =>
        HandleAndDrainAsync(new CatalogSearchAttemptDto("search:track:rare unknown song", "rare unknown song", "spotify,appleMusic,youtubeMusic", 0, 100, DefaultOccurredAt, "corr-1"));

    private async Task HandleAndDrainAsync(CatalogSearchAttemptDto dto)
    {
        await Listener.Handle(dto, null!);

        foreach (var command in commandBus.SentCommands)
        {
            switch (command)
            {
                case RecordCatalogSearchCandidateCommand candidate:
                    await recordCandidateHandler.Handle(candidate, CancellationToken.None);
                    break;
            }
        }
    }
}
