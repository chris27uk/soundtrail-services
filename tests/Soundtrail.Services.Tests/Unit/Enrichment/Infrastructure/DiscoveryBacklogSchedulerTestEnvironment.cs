using Soundtrail.Contracts.Common;
using Soundtrail.Domain;
using Soundtrail.Domain.Catalog;
using Soundtrail.Domain.Commands;
using Soundtrail.Domain.Discovery;
using Soundtrail.Services.Enrichment.Orchestrator.Features.BacklogScheduling;
using Soundtrail.Services.Enrichment.Orchestrator.Features.BacklogScheduling.Support;
using Soundtrail.Services.Enrichment.Orchestrator.Shared.Prioritisation;

namespace Soundtrail.Services.Tests.Unit.Enrichment.Infrastructure;

internal sealed class DiscoveryBacklogSchedulerTestEnvironment
{
    private static readonly DateTimeOffset DefaultNow = new(2026, 5, 31, 12, 0, 0, TimeSpan.Zero);

    private readonly PotentialCatalogLookupWorkStoreFake store;
    private readonly LocalMusicTrackSearchFake localSearch;
    private readonly SourceApiBudgetPortFake sourceBudget;
    private readonly CatalogSearchTrackingStoreFake catalogSearchTrackingStoreFake;
    private readonly CatalogSearchDiscoveryRepositoryFake catalogSearchDiscoveryRepositoryFake;
    private readonly CommandBusFake commandBusFake;

    private DiscoveryBacklogSchedulerTestEnvironment(params Soundtrail.Services.Enrichment.Orchestrator.Shared.Persistence.PotentialCatalogLookupWork[] candidates)
    {
        this.store = new PotentialCatalogLookupWorkStoreFake();
        foreach (var candidate in candidates)
        {
            this.store.Seed(candidate);
        }

        ActiveWorkStore = new ActiveLookupWorkStoreFake();
        localSearch = new LocalMusicTrackSearchFake();
        sourceBudget = new SourceApiBudgetPortFake();
        catalogSearchTrackingStoreFake = new CatalogSearchTrackingStoreFake();
        catalogSearchDiscoveryRepositoryFake = new CatalogSearchDiscoveryRepositoryFake();
        commandBusFake = new CommandBusFake();
        foreach (var candidate in candidates)
        {
            localSearch.Seed(new LocalMusicTrackSearchResult(
                candidate.MusicCatalogId,
                $"Track {candidate.MusicCatalogId.Value}",
                $"Artist {candidate.MusicCatalogId.Value}",
                $"Album {candidate.MusicCatalogId.Value}",
                null,
                null,
                null,
                IsPlayable: false));
            catalogSearchTrackingStoreFake.Seed(new CatalogSearchTracking(
                CatalogSearchCriteria.Track(TrackId.From(candidate.MusicCatalogId.Value)),
                candidate.MusicCatalogId,
                DefaultNow));
        }

        Scheduler = new DiscoveryBacklogScheduler(
            this.store,
            ActiveWorkStore,
            new DiscoveryPriorityPolicy(),
            sourceBudget,
            localSearch,
            new DiscoveryBacklogLookupPlanner(),
            new TrackedDiscoveryStartMarker(
                catalogSearchTrackingStoreFake,
                catalogSearchDiscoveryRepositoryFake),
            commandBusFake);
        Now = DefaultNow;
    }

    public DiscoveryBacklogScheduler Scheduler { get; }

    public ActiveLookupWorkStoreFake ActiveWorkStore { get; }

    public DateTimeOffset Now { get; }

    public SourceApiBudgetPortFake SourceBudget => sourceBudget;

    public CommandBusFake CommandBus => commandBusFake;

    public static DiscoveryBacklogSchedulerTestEnvironment WithHighAndLowPriorityEligibleCandidates() =>
        new(
            Candidates.PopularEligibleCandidate(),
            Candidates.LowDemandEligibleCandidate());

    public static DiscoveryBacklogSchedulerTestEnvironment WithActiveWorkForPopularCandidate()
    {
        var env = new DiscoveryBacklogSchedulerTestEnvironment(Candidates.PopularEligibleCandidate());
        env.ActiveWorkStore.TryAcquireAsync(
            CommandId.For("LookupCanonicalMusicMetadata:mc_track_high"),
            env.Now.AddMinutes(5),
            CancellationToken.None).GetAwaiter().GetResult();
        return env;
    }

    public static DiscoveryBacklogSchedulerTestEnvironment WithMoreEligibleCandidatesThanTheBatchSize() =>
        new(
            Candidates.PopularEligibleCandidate("mc_track_1"),
            Candidates.ExistingCandidate(MusicCatalogId.From("mc_track_2"), requestCount: 2, highestTrustLevelSeen: 1),
            Candidates.ExistingCandidate(MusicCatalogId.From("mc_track_3"), requestCount: 1, highestTrustLevelSeen: 0));

    public static DiscoveryBacklogSchedulerTestEnvironment WithMediumRiskCandidate() =>
        new(Candidates.MediumRiskCandidate());

    public static DiscoveryBacklogSchedulerTestEnvironment WithHighTrustLowDemandCandidate() =>
        new(Candidates.HighTrustLowDemandCandidate());

    public static DiscoveryBacklogSchedulerTestEnvironment WithHighRiskCandidate() =>
        new(Candidates.HighRiskCandidate());

    public static DiscoveryBacklogSchedulerTestEnvironment WithNotYetEligibleCandidate() =>
        new(Candidates.NotYetEligibleCandidate(MusicCatalogId.From("mc_track_deferred")));

    public static DiscoveryBacklogSchedulerTestEnvironment WithResolvedCandidate() =>
        new(Candidates.ResolvedCandidate());

    public static DiscoveryBacklogSchedulerTestEnvironment WithScheduledCandidate() =>
        new(Candidates.EligibleCandidate());

    public LocalMusicTrackSearchFake LocalSearch => localSearch;

    public Task RunSweep(int take = 10) => Scheduler.RunOnceAsync(Now, take);
}
