using FluentAssertions;
using Soundtrail.Services.Enrichment.DiscoveryPlanner.Features.BacklogScheduling;
using Soundtrail.Services.Enrichment.DiscoveryPlanner.Shared.Persistence;
using Soundtrail.Services.Enrichment.DiscoveryPlanner.Shared.Prioritisation;
using Soundtrail.Contracts.Common;
using Soundtrail.Services.Tests.Unit.Enrichment.Infrastructure;

namespace Soundtrail.Services.Tests.Unit.Enrichment.Features.Scheduling;

public class DiscoveryBacklogSchedulerTests
{
    [Fact]
    public async Task Given_Eligible_Candidates_When_Sweep_Runs_Then_Two_Commands_Are_Returned()
    {
        var commands = await RunSweepWithHighAndLowPriorityCandidates();

        commands.Should().HaveCount(2);
    }

    [Fact]
    public async Task Given_Eligible_Candidates_When_Sweep_Runs_Then_A_High_Priority_Command_Is_Returned_For_The_Popular_Candidate()
    {
        var commands = await RunSweepWithHighAndLowPriorityCandidates();

        commands.Should().ContainSingle(command => command.MusicCatalogId == "mc_track_high" && command.Priority == LookupPriorityBand.High);
    }

    [Fact]
    public async Task Given_Eligible_Candidates_When_Sweep_Runs_Then_A_Low_Priority_Command_Is_Returned_For_The_Low_Demand_Candidate()
    {
        var commands = await RunSweepWithHighAndLowPriorityCandidates();

        commands.Should().ContainSingle(command => command.MusicCatalogId == "mc_track_low" && command.Priority == LookupPriorityBand.Low);
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
        await activeWorkStore.TryAcquireAsync(CommandId.For(musicCatalogId.Value), now.AddMinutes(5), CancellationToken.None);
        var sweep = new DiscoveryBacklogScheduler(store, activeWorkStore, new DiscoveryPriorityPolicy());

        var commands = await sweep.RunOnceAsync(now, 10);

        commands.Should().BeEmpty();
    }

    [Fact]
    public async Task Given_More_Eligible_Candidates_Than_The_Batch_Size_When_Scheduling_Backlog_Then_Only_The_Top_Batch_Is_Returned()
    {
        var now = new DateTimeOffset(2026, 5, 31, 12, 0, 0, TimeSpan.Zero);
        var store = new RankedMusicCandidateStoreFake();
        store.Seed(Candidates.ExistingCandidate(
            MusicCatalogId.From("mc_track_1"),
            requestCount: 3,
            highestTrustLevelSeen: 2));
        store.Seed(Candidates.ExistingCandidate(
            MusicCatalogId.From("mc_track_2"),
            requestCount: 2,
            highestTrustLevelSeen: 1));
        store.Seed(Candidates.ExistingCandidate(
            MusicCatalogId.From("mc_track_3"),
            requestCount: 1,
            highestTrustLevelSeen: 0));

        var activeWorkStore = new ActiveLookupWorkStoreFake();
        var scheduler = new DiscoveryBacklogScheduler(store, activeWorkStore, new DiscoveryPriorityPolicy());

        var commands = await scheduler.RunOnceAsync(now, 2);

        commands.Should().HaveCount(2);
    }

    [Fact]
    public async Task Given_A_Medium_Risk_Candidate_When_Scheduling_Backlog_Then_A_Low_Priority_Command_Is_Returned()
    {
        var now = new DateTimeOffset(2026, 5, 31, 12, 0, 0, TimeSpan.Zero);
        var store = new RankedMusicCandidateStoreFake();
        store.Seed(Candidates.ExistingCandidate(
            MusicCatalogId.From("mc_track_medium"),
            requestCount: 5,
            highestTrustLevelSeen: 3,
            riskScore: 30));

        var activeWorkStore = new ActiveLookupWorkStoreFake();
        var scheduler = new DiscoveryBacklogScheduler(store, activeWorkStore, new DiscoveryPriorityPolicy());

        var commands = await scheduler.RunOnceAsync(now, 10);

        commands.Should().ContainSingle(command => command.Priority == LookupPriorityBand.Low);
    }

    [Fact]
    public async Task Given_A_High_Trust_Low_Demand_Candidate_When_Scheduling_Backlog_Then_A_High_Priority_Command_Is_Returned()
    {
        var now = new DateTimeOffset(2026, 5, 31, 12, 0, 0, TimeSpan.Zero);
        var store = new RankedMusicCandidateStoreFake();
        store.Seed(Candidates.ExistingCandidate(
            MusicCatalogId.From("mc_track_trusted"),
            requestCount: 1,
            highestTrustLevelSeen: 2,
            riskScore: 10));

        var activeWorkStore = new ActiveLookupWorkStoreFake();
        var scheduler = new DiscoveryBacklogScheduler(store, activeWorkStore, new DiscoveryPriorityPolicy());

        var commands = await scheduler.RunOnceAsync(now, 10);

        commands.Should().ContainSingle(command => command.Priority == LookupPriorityBand.High);
    }

    [Fact]
    public async Task Given_A_High_Risk_Candidate_When_Scheduling_Backlog_Then_No_Command_Is_Returned()
    {
        var now = new DateTimeOffset(2026, 5, 31, 12, 0, 0, TimeSpan.Zero);
        var store = new RankedMusicCandidateStoreFake();
        store.Seed(Candidates.ExistingCandidate(
            MusicCatalogId.From("mc_track_high_risk"),
            requestCount: 2,
            highestTrustLevelSeen: 1,
            riskScore: 60));

        var activeWorkStore = new ActiveLookupWorkStoreFake();
        var scheduler = new DiscoveryBacklogScheduler(store, activeWorkStore, new DiscoveryPriorityPolicy());

        var commands = await scheduler.RunOnceAsync(now, 10);

        commands.Should().BeEmpty();
    }

    [Fact]
    public async Task Given_A_Not_Yet_Eligible_Candidate_When_Scheduling_Backlog_Then_No_Command_Is_Returned()
    {
        var now = new DateTimeOffset(2026, 5, 31, 12, 0, 0, TimeSpan.Zero);
        var store = new RankedMusicCandidateStoreFake();
        store.Seed(Candidates.ExistingCandidate(
            MusicCatalogId.From("mc_track_deferred"),
            requestCount: 2,
            highestTrustLevelSeen: 1,
            riskScore: 10,
            status: RankedMusicCandidateStatus.Pending,
            nextEligibleAt: now.AddMinutes(1)));

        var activeWorkStore = new ActiveLookupWorkStoreFake();
        var scheduler = new DiscoveryBacklogScheduler(store, activeWorkStore, new DiscoveryPriorityPolicy());

        var commands = await scheduler.RunOnceAsync(now, 10);

        commands.Should().BeEmpty();
    }

    [Fact]
    public async Task Given_A_Resolved_Candidate_When_Scheduling_Backlog_Then_No_Command_Is_Returned()
    {
        var now = new DateTimeOffset(2026, 5, 31, 12, 0, 0, TimeSpan.Zero);
        var store = new RankedMusicCandidateStoreFake();
        store.Seed(Candidates.ExistingCandidate(
            MusicCatalogId.From("mc_track_resolved"),
            requestCount: 2,
            highestTrustLevelSeen: 1,
            riskScore: 10,
            status: RankedMusicCandidateStatus.Resolved,
            nextEligibleAt: null));

        var activeWorkStore = new ActiveLookupWorkStoreFake();
        var scheduler = new DiscoveryBacklogScheduler(store, activeWorkStore, new DiscoveryPriorityPolicy());

        var commands = await scheduler.RunOnceAsync(now, 10);

        commands.Should().BeEmpty();
    }

    [Fact]
    public async Task Given_A_Scheduled_Candidate_When_Scheduling_Backlog_Then_Command_CreatedAt_Matches_The_Scheduling_Time()
    {
        var now = new DateTimeOffset(2026, 5, 31, 12, 0, 0, TimeSpan.Zero);
        var store = new RankedMusicCandidateStoreFake();
        store.Seed(Candidates.ExistingCandidate(MusicCatalogId.From("mc_track_1")));

        var activeWorkStore = new ActiveLookupWorkStoreFake();
        var scheduler = new DiscoveryBacklogScheduler(store, activeWorkStore, new DiscoveryPriorityPolicy());

        var commands = await scheduler.RunOnceAsync(now, 10);

        commands[0].CreatedAt.Should().Be(now);
    }

    [Fact]
    public async Task Given_A_Scheduled_Candidate_When_Scheduling_Backlog_Then_Command_CorrelationId_Is_Populated()
    {
        var now = new DateTimeOffset(2026, 5, 31, 12, 0, 0, TimeSpan.Zero);
        var store = new RankedMusicCandidateStoreFake();
        store.Seed(Candidates.ExistingCandidate(MusicCatalogId.From("mc_track_1")));

        var activeWorkStore = new ActiveLookupWorkStoreFake();
        var scheduler = new DiscoveryBacklogScheduler(store, activeWorkStore, new DiscoveryPriorityPolicy());

        var commands = await scheduler.RunOnceAsync(now, 10);

        commands[0].CorrelationId.Value.Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public async Task Given_A_Scheduled_Candidate_When_Scheduling_Backlog_Then_CommandId_Is_Built_From_The_MusicCatalogId()
    {
        var now = new DateTimeOffset(2026, 5, 31, 12, 0, 0, TimeSpan.Zero);
        var store = new RankedMusicCandidateStoreFake();
        store.Seed(Candidates.ExistingCandidate(MusicCatalogId.From("mc_track_1")));

        var activeWorkStore = new ActiveLookupWorkStoreFake();
        var scheduler = new DiscoveryBacklogScheduler(store, activeWorkStore, new DiscoveryPriorityPolicy());

        var commands = await scheduler.RunOnceAsync(now, 10);

        commands[0].CommandId.Should().Be(CommandId.For("mc_track_1"));
    }

    private static async Task<IReadOnlyList<Soundtrail.Services.Enrichment.DiscoveryPlanner.Features.JustInTimeScheduling.Model.LookupMusicCommand>> RunSweepWithHighAndLowPriorityCandidates()
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
        var sweep = new DiscoveryBacklogScheduler(store, activeWorkStore, new DiscoveryPriorityPolicy());

        return await sweep.RunOnceAsync(now, 10);
    }
}
