using Soundtrail.Contracts.Common;
using Soundtrail.Domain.Discovery;
using Soundtrail.Domain.Enrichment.Commands;
using Soundtrail.Services.Enrichment.Orchestrator.Features.OnNextMusicTracksRequestedForLookup;

namespace Soundtrail.Services.Tests.Unit.Enrichment.Infrastructure;

internal sealed class NextMusicTracksRequestedForLookupHandlerTestEnvironment
{
    private static readonly DateTimeOffset DefaultNow = new(2026, 5, 31, 12, 0, 0, TimeSpan.Zero);

    private readonly DiscoveryBacklogPlanningReadPortFake store;
    private readonly CommandBusFake commandBusFake;

    private NextMusicTracksRequestedForLookupHandlerTestEnvironment(params DiscoveryBacklogCandidate[] candidates)
    {
        this.store = new DiscoveryBacklogPlanningReadPortFake();
        this.store.Seed(candidates);
        commandBusFake = new CommandBusFake();
        Scheduler = new NextMusicTracksRequestedForLookupHandler(this.store, commandBusFake);
        Now = DefaultNow;
    }

    public NextMusicTracksRequestedForLookupHandler Scheduler { get; }

    public DateTimeOffset Now { get; }

    public CommandBusFake CommandBus => commandBusFake;

    public static NextMusicTracksRequestedForLookupHandlerTestEnvironment WithHighAndLowPriorityEligibleCandidates() =>
        new(
            Candidate("rare popular song", "mc_track_high"),
            Candidate("rare low demand song", "mc_track_low"));

    public static NextMusicTracksRequestedForLookupHandlerTestEnvironment WithMoreEligibleCandidatesThanTheBatchSize() =>
        new(
            Candidate("candidate one", "mc_track_1"),
            Candidate("candidate two", "mc_track_2"),
            Candidate("candidate three", "mc_track_3"));

    public static NextMusicTracksRequestedForLookupHandlerTestEnvironment WithNotYetEligibleCandidate() =>
        new(new DiscoveryBacklogCandidate(
            MusicSearchCriteria.ByQuery("not yet eligible", SearchTypesFilter.Tracks),
            MusicCatalogId.From("mc_track_deferred"),
            DefaultNow,
            DefaultNow.AddMinutes(5)));

    public Task RunSweep(int take = 10) => Scheduler.Handle(
        new RunDiscoveryBacklogSchedulingCommand(
            CommandId.For($"RunDiscoveryBacklogScheduling:{Now.ToUnixTimeMilliseconds()}"),
            Now,
            CorrelationId.New(),
            take),
        CancellationToken.None);

    private static DiscoveryBacklogCandidate Candidate(string query, string musicCatalogId) =>
        new(
            MusicSearchCriteria.ByQuery(query, SearchTypesFilter.Tracks),
            MusicCatalogId.From(musicCatalogId),
            DefaultNow,
            null);
}
