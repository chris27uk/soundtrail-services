using FluentAssertions;
using Soundtrail.Services.Enrichment.Features.Scheduling;
using Soundtrail.Services.Enrichment.Features.Scheduling.Models;
using Soundtrail.Services.Tests.Enrichment.Unit.Infrastructure;

namespace Soundtrail.Services.Tests.Enrichment.Unit.Features.Scheduling;

public class LookupPlanningSweepTests
{
    [Fact]
    public async Task Given_Eligible_Candidates_When_Sweep_Runs_Then_High_And_Low_Priority_Commands_Are_Queued()
    {
        var now = new DateTimeOffset(2026, 5, 31, 12, 0, 0, TimeSpan.Zero);
        var store = new RankedMusicCandidateStoreFake();
        store.Seed(Candidates.ExistingCandidate(
            MusicCatalogId.From("mc_track_high"),
            requestCount: 3,
            highestTrustLevelSeen: 2,
            riskScore: 10,
            status: RankedMusicCandidateStatus.Pending,
            nextEligibleAt: null));
        store.Seed(Candidates.ExistingCandidate(
            MusicCatalogId.From("mc_track_low"),
            requestCount: 1,
            highestTrustLevelSeen: 1,
            riskScore: 10,
            status: RankedMusicCandidateStatus.Pending,
            nextEligibleAt: null));

        var activeWorkStore = new ActiveLookupWorkStoreFake();
        var queue = new LookupMusicCommandQueueFake();
        var sweep = new LookupPlanningSweep(store, activeWorkStore, queue, new LookupPlanner());

        var emitted = await sweep.RunOnceAsync(now, 10);

        emitted.Should().Be(2);
        queue.Commands.Should().HaveCount(2);
        queue.Commands.Should().ContainSingle(command => command.MusicCatalogId.Value == "mc_track_high" && command.Priority == LookupPriorityBand.High);
        queue.Commands.Should().ContainSingle(command => command.MusicCatalogId.Value == "mc_track_low" && command.Priority == LookupPriorityBand.Low);
    }

    [Fact]
    public async Task Given_Active_Work_Exists_When_Sweep_Runs_Then_That_Candidate_Is_Skipped()
    {
        var now = new DateTimeOffset(2026, 5, 31, 12, 0, 0, TimeSpan.Zero);
        var musicCatalogId = MusicCatalogId.From("mc_track_high");
        var store = new RankedMusicCandidateStoreFake();
        store.Seed(Candidates.ExistingCandidate(
            musicCatalogId,
            requestCount: 3,
            highestTrustLevelSeen: 2,
            riskScore: 10,
            status: RankedMusicCandidateStatus.Pending,
            nextEligibleAt: null));

        var activeWorkStore = new ActiveLookupWorkStoreFake();
        await activeWorkStore.TryReserveAsync(musicCatalogId, "existing", now.AddMinutes(5), CancellationToken.None);
        var queue = new LookupMusicCommandQueueFake();
        var sweep = new LookupPlanningSweep(store, activeWorkStore, queue, new LookupPlanner());

        var emitted = await sweep.RunOnceAsync(now, 10);

        emitted.Should().Be(0);
        queue.Commands.Should().BeEmpty();
    }
}
