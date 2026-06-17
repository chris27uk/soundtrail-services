using Soundtrail.Contracts.Common;
using Soundtrail.Domain;
using Soundtrail.Services.Enrichment.DiscoveryPlanner.Features.BacklogScheduling;
using Soundtrail.Services.Enrichment.DiscoveryPlanner.Features.JustInTimeScheduling.LocalSearch;
using Soundtrail.Services.Enrichment.DiscoveryPlanner.Features.JustInTimeScheduling.Model;
using Soundtrail.Services.Enrichment.DiscoveryPlanner.Shared.Prioritisation;

namespace Soundtrail.Services.Tests.Unit.Enrichment.Infrastructure;

internal sealed class DiscoveryBacklogSchedulerTestEnvironment
{
    private static readonly DateTimeOffset DefaultNow = new(2026, 5, 31, 12, 0, 0, TimeSpan.Zero);

    private readonly RankedMusicCandidateStoreFake store;
    private readonly LocalMusicTrackSearchFake localSearch;

    private DiscoveryBacklogSchedulerTestEnvironment(params Soundtrail.Services.Enrichment.DiscoveryPlanner.Shared.Persistence.RankedMusicCandidate[] candidates)
    {
        this.store = new RankedMusicCandidateStoreFake();
        foreach (var candidate in candidates)
        {
            this.store.Seed(candidate);
        }

        ActiveWorkStore = new ActiveLookupWorkStoreFake();
        localSearch = new LocalMusicTrackSearchFake();
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
        }

        Scheduler = new DiscoveryBacklogScheduler(this.store, ActiveWorkStore, new DiscoveryPriorityPolicy(), localSearch);
        Now = DefaultNow;
    }

    public DiscoveryBacklogScheduler Scheduler { get; }

    public ActiveLookupWorkStoreFake ActiveWorkStore { get; }

    public DateTimeOffset Now { get; }

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

    public Task<IReadOnlyList<LookupPhaseCommand>> RunSweep(int take = 10) => Scheduler.RunOnceAsync(Now, take);
}
