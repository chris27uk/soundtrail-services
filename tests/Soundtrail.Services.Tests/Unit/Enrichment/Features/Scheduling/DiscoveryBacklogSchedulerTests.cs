using FluentAssertions;
using Soundtrail.Contracts.Common;
using Soundtrail.Services.Tests.Unit.Enrichment.Infrastructure;

namespace Soundtrail.Services.Tests.Unit.Enrichment.Features.Scheduling;

public sealed class DiscoveryBacklogSchedulerTests
{
    [Fact]
    public async Task Given_Eligible_Candidates_When_Sweep_Runs_Then_Two_Commands_Are_Returned()
    {
        var env = DiscoveryBacklogSchedulerTestEnvironment.WithHighAndLowPriorityEligibleCandidates();
        var commands = await env.RunSweep();
        commands.Should().HaveCount(2);
    }

    [Fact]
    public async Task Given_Eligible_Candidates_When_Sweep_Runs_Then_A_High_Priority_Command_Is_Returned_For_The_Popular_Candidate()
    {
        var env = DiscoveryBacklogSchedulerTestEnvironment.WithHighAndLowPriorityEligibleCandidates();
        var commands = await env.RunSweep();
        commands.Should().ContainSingle(command => command.MusicCatalogId == "mc_track_high" && command.Priority == LookupPriorityBand.High);
    }

    [Fact]
    public async Task Given_Eligible_Candidates_When_Sweep_Runs_Then_A_Low_Priority_Command_Is_Returned_For_The_Low_Demand_Candidate()
    {
        var env = DiscoveryBacklogSchedulerTestEnvironment.WithHighAndLowPriorityEligibleCandidates();
        var commands = await env.RunSweep();
        commands.Should().ContainSingle(command => command.MusicCatalogId == "mc_track_low" && command.Priority == LookupPriorityBand.Low);
    }

    [Fact]
    public async Task Given_Active_Work_Exists_When_Sweep_Runs_Then_That_Candidate_Is_Skipped()
    {
        var env = DiscoveryBacklogSchedulerTestEnvironment.WithActiveWorkForPopularCandidate();
        var commands = await env.RunSweep();
        commands.Should().BeEmpty();
    }

    [Fact]
    public async Task Given_More_Eligible_Candidates_Than_The_Batch_Size_When_Scheduling_Backlog_Then_Only_The_Top_Batch_Is_Returned()
    {
        var env = DiscoveryBacklogSchedulerTestEnvironment.WithMoreEligibleCandidatesThanTheBatchSize();
        var commands = await env.RunSweep(take: 2);
        commands.Should().HaveCount(2);
    }

    [Fact]
    public async Task Given_A_Medium_Risk_Candidate_When_Scheduling_Backlog_Then_A_Low_Priority_Command_Is_Returned()
    {
        var env = DiscoveryBacklogSchedulerTestEnvironment.WithMediumRiskCandidate();
        var commands = await env.RunSweep();
        commands.Should().ContainSingle(command => command.Priority == LookupPriorityBand.Low);
    }

    [Fact]
    public async Task Given_A_High_Trust_Low_Demand_Candidate_When_Scheduling_Backlog_Then_A_High_Priority_Command_Is_Returned()
    {
        var env = DiscoveryBacklogSchedulerTestEnvironment.WithHighTrustLowDemandCandidate();
        var commands = await env.RunSweep();
        commands.Should().ContainSingle(command => command.Priority == LookupPriorityBand.High);
    }

    [Fact]
    public async Task Given_A_High_Risk_Candidate_When_Scheduling_Backlog_Then_No_Command_Is_Returned()
    {
        var env = DiscoveryBacklogSchedulerTestEnvironment.WithHighRiskCandidate();
        var commands = await env.RunSweep();
        commands.Should().BeEmpty();
    }

    [Fact]
    public async Task Given_A_Not_Yet_Eligible_Candidate_When_Scheduling_Backlog_Then_No_Command_Is_Returned()
    {
        var env = DiscoveryBacklogSchedulerTestEnvironment.WithNotYetEligibleCandidate();
        var commands = await env.RunSweep();
        commands.Should().BeEmpty();
    }

    [Fact]
    public async Task Given_A_Resolved_Candidate_When_Scheduling_Backlog_Then_No_Command_Is_Returned()
    {
        var env = DiscoveryBacklogSchedulerTestEnvironment.WithResolvedCandidate();
        var commands = await env.RunSweep();
        commands.Should().BeEmpty();
    }

    [Fact]
    public async Task Given_A_Scheduled_Candidate_When_Scheduling_Backlog_Then_Command_CreatedAt_Matches_The_Scheduling_Time()
    {
        var env = DiscoveryBacklogSchedulerTestEnvironment.WithScheduledCandidate();
        var commands = await env.RunSweep();
        commands[0].CreatedAt.Should().Be(env.Now);
    }

    [Fact]
    public async Task Given_A_Scheduled_Candidate_When_Scheduling_Backlog_Then_Command_CorrelationId_Is_Populated()
    {
        var env = DiscoveryBacklogSchedulerTestEnvironment.WithScheduledCandidate();
        var commands = await env.RunSweep();
        commands[0].CorrelationId.Value.Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public async Task Given_A_Scheduled_Candidate_When_Scheduling_Backlog_Then_CommandId_Is_Built_From_The_MusicCatalogId()
    {
        var env = DiscoveryBacklogSchedulerTestEnvironment.WithScheduledCandidate();
        var commands = await env.RunSweep();
        commands[0].CommandId.Should().Be(CommandId.For("LookupCanonicalMusicMetadata:mc_track_1"));
    }

    [Fact]
    public async Task Given_A_Source_Budget_Rejection_When_Scheduling_Backlog_Then_No_Command_Is_Returned()
    {
        var env = DiscoveryBacklogSchedulerTestEnvironment.WithScheduledCandidate();
        env.SourceBudget.Reject(
            ProviderName.MusicBrainz,
            env.Now.AddMinutes(1),
            "MusicBrainz budget temporarily unavailable");

        var commands = await env.RunSweep();

        commands.Should().BeEmpty();
    }
}
