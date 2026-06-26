using Soundtrail.Contracts.Common;
using Soundtrail.Domain;
using Soundtrail.Domain.Catalog;
using Soundtrail.Domain.Commands;
using Soundtrail.Services.Enrichment.Orchestrator.Features.OnNextMusicTracksRequestedForLookup;
using Soundtrail.Services.Enrichment.Orchestrator.Shared.Persistence;

namespace Soundtrail.Services.Tests.Unit.Enrichment.Infrastructure;

internal sealed class NextMusicTracksRequestedForLookupHandlerTestEnvironment
{
    private static readonly DateTimeOffset DefaultNow = new(2026, 5, 31, 12, 0, 0, TimeSpan.Zero);

    private readonly PotentialCatalogLookupWorkStoreFake store;
    private readonly CommandBusFake commandBusFake;

    private NextMusicTracksRequestedForLookupHandlerTestEnvironment(params PotentialCatalogLookupWork[] candidates)
    {
        this.store = new PotentialCatalogLookupWorkStoreFake();
        foreach (var candidate in candidates)
        {
            this.store.Seed(candidate);
        }

        ActiveWorkStore = new ActiveLookupWorkStoreFake();
        commandBusFake = new CommandBusFake();
        Scheduler = new NextMusicTracksRequestedForLookupHandler(this.store, commandBusFake);
        Now = DefaultNow;
    }

    public NextMusicTracksRequestedForLookupHandler Scheduler { get; }

    public ActiveLookupWorkStoreFake ActiveWorkStore { get; }

    public DateTimeOffset Now { get; }

    public CommandBusFake CommandBus => commandBusFake;

    public static NextMusicTracksRequestedForLookupHandlerTestEnvironment WithHighAndLowPriorityEligibleCandidates() =>
        new(
            Candidates.PopularEligibleCandidate(),
            Candidates.LowDemandEligibleCandidate());

    public static NextMusicTracksRequestedForLookupHandlerTestEnvironment WithActiveWorkForPopularCandidate()
    {
        var env = new NextMusicTracksRequestedForLookupHandlerTestEnvironment(Candidates.PopularEligibleCandidate());
        env.ActiveWorkStore.TryAcquireAsync(
            CommandId.For("LookupMusicMetadata:mc_track_high"),
            env.Now.AddMinutes(5),
            CancellationToken.None).GetAwaiter().GetResult();
        return env;
    }

    public static NextMusicTracksRequestedForLookupHandlerTestEnvironment WithMoreEligibleCandidatesThanTheBatchSize() =>
        new(
            Candidates.PopularEligibleCandidate("mc_track_1"),
            Candidates.ExistingCandidate(MusicCatalogId.From("mc_track_2"), requestCount: 2, highestTrustLevelSeen: 1),
            Candidates.ExistingCandidate(MusicCatalogId.From("mc_track_3"), requestCount: 1, highestTrustLevelSeen: 0));

    public static NextMusicTracksRequestedForLookupHandlerTestEnvironment WithMediumRiskCandidate() =>
        new(Candidates.MediumRiskCandidate());

    public static NextMusicTracksRequestedForLookupHandlerTestEnvironment WithHighTrustLowDemandCandidate() =>
        new(Candidates.HighTrustLowDemandCandidate());

    public static NextMusicTracksRequestedForLookupHandlerTestEnvironment WithHighRiskCandidate() =>
        new(Candidates.HighRiskCandidate());

    public static NextMusicTracksRequestedForLookupHandlerTestEnvironment WithNotYetEligibleCandidate() =>
        new(Candidates.NotYetEligibleCandidate(MusicCatalogId.From("mc_track_deferred")));

    public static NextMusicTracksRequestedForLookupHandlerTestEnvironment WithResolvedCandidate() =>
        new(Candidates.ResolvedCandidate());

    public static NextMusicTracksRequestedForLookupHandlerTestEnvironment WithScheduledCandidate() =>
        new(Candidates.EligibleCandidate());

    public Task RunSweep(int take = 10) => Scheduler.RunOnceAsync(Now, take);
}
