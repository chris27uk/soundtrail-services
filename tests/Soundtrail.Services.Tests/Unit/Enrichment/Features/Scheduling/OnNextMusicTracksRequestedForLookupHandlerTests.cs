using FluentAssertions;
using Soundtrail.Contracts.Common;
using Soundtrail.Domain.Commands;
using Soundtrail.Services.Tests.Unit.Enrichment.Infrastructure;

namespace Soundtrail.Services.Tests.Unit.Enrichment.Features.Scheduling;

public sealed class NextMusicTracksRequestedForLookupHandlerTests
{
    [Fact]
    public async Task Given_Eligible_Candidates_When_Sweep_Runs_Then_Two_Commands_Are_Returned()
    {
        var env = NextMusicTracksRequestedForLookupHandlerTestEnvironment.WithHighAndLowPriorityEligibleCandidates();
        await env.RunSweep();
        env.CommandBus.SentCommands.Should().HaveCount(2);
    }

    [Fact]
    public async Task Given_Eligible_Candidates_When_Sweep_Runs_Then_A_High_Priority_Command_Is_Returned_For_The_Popular_Candidate()
    {
        var env = NextMusicTracksRequestedForLookupHandlerTestEnvironment.WithHighAndLowPriorityEligibleCandidates();
        await env.RunSweep();
        env.CommandBus.SentCommands.OfType<IMusicCatalogLookupCommand>()
            .Should().ContainSingle(command => command.MusicCatalogId == "mc_track_high" && command.Priority == LookupPriorityBand.High);
    }

    [Fact]
    public async Task Given_Eligible_Candidates_When_Sweep_Runs_Then_A_Low_Priority_Command_Is_Returned_For_The_Low_Demand_Candidate()
    {
        var env = NextMusicTracksRequestedForLookupHandlerTestEnvironment.WithHighAndLowPriorityEligibleCandidates();
        await env.RunSweep();
        env.CommandBus.SentCommands.OfType<IMusicCatalogLookupCommand>()
            .Should().ContainSingle(command => command.MusicCatalogId == "mc_track_low" && command.Priority == LookupPriorityBand.Low);
    }

    [Fact]
    public async Task Given_Active_Work_Exists_When_Sweep_Runs_Then_That_Candidate_Is_Skipped()
    {
        var env = NextMusicTracksRequestedForLookupHandlerTestEnvironment.WithActiveWorkForPopularCandidate();
        await env.RunSweep();
        env.CommandBus.SentCommands.Should().BeEmpty();
    }

    [Fact]
    public async Task Given_More_Eligible_Candidates_Than_The_Batch_Size_When_Scheduling_Backlog_Then_Only_The_Top_Batch_Is_Returned()
    {
        var env = NextMusicTracksRequestedForLookupHandlerTestEnvironment.WithMoreEligibleCandidatesThanTheBatchSize();
        await env.RunSweep(take: 2);
        env.CommandBus.SentCommands.Should().HaveCount(2);
    }

    [Fact]
    public async Task Given_A_Medium_Risk_Candidate_When_Scheduling_Backlog_Then_A_Low_Priority_Command_Is_Returned()
    {
        var env = NextMusicTracksRequestedForLookupHandlerTestEnvironment.WithMediumRiskCandidate();
        await env.RunSweep();
        env.CommandBus.SentCommands.OfType<IMusicCatalogLookupCommand>()
            .Should().ContainSingle(command => command.Priority == LookupPriorityBand.Low);
    }

    [Fact]
    public async Task Given_A_High_Trust_Low_Demand_Candidate_When_Scheduling_Backlog_Then_A_High_Priority_Command_Is_Returned()
    {
        var env = NextMusicTracksRequestedForLookupHandlerTestEnvironment.WithHighTrustLowDemandCandidate();
        await env.RunSweep();
        env.CommandBus.SentCommands.OfType<IMusicCatalogLookupCommand>()
            .Should().ContainSingle(command => command.Priority == LookupPriorityBand.High);
    }

    [Fact]
    public async Task Given_A_High_Risk_Candidate_When_Scheduling_Backlog_Then_No_Command_Is_Returned()
    {
        var env = NextMusicTracksRequestedForLookupHandlerTestEnvironment.WithHighRiskCandidate();
        await env.RunSweep();
        env.CommandBus.SentCommands.Should().BeEmpty();
    }

    [Fact]
    public async Task Given_A_Not_Yet_Eligible_Candidate_When_Scheduling_Backlog_Then_No_Command_Is_Returned()
    {
        var env = NextMusicTracksRequestedForLookupHandlerTestEnvironment.WithNotYetEligibleCandidate();
        await env.RunSweep();
        env.CommandBus.SentCommands.Should().BeEmpty();
    }

    [Fact]
    public async Task Given_A_Resolved_Candidate_When_Scheduling_Backlog_Then_No_Command_Is_Returned()
    {
        var env = NextMusicTracksRequestedForLookupHandlerTestEnvironment.WithResolvedCandidate();
        await env.RunSweep();
        env.CommandBus.SentCommands.Should().BeEmpty();
    }

    [Fact]
    public async Task Given_A_Scheduled_Candidate_When_Scheduling_Backlog_Then_Command_CreatedAt_Matches_The_Scheduling_Time()
    {
        var env = NextMusicTracksRequestedForLookupHandlerTestEnvironment.WithScheduledCandidate();
        await env.RunSweep();
        env.CommandBus.SentCommands.OfType<IMusicCatalogLookupCommand>().Single().CreatedAt.Should().Be(env.Now);
    }

    [Fact]
    public async Task Given_A_Scheduled_Candidate_When_Scheduling_Backlog_Then_Command_CorrelationId_Is_Populated()
    {
        var env = NextMusicTracksRequestedForLookupHandlerTestEnvironment.WithScheduledCandidate();
        await env.RunSweep();
        env.CommandBus.SentCommands.OfType<IMusicCatalogLookupCommand>().Single().CorrelationId.Value.Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public async Task Given_A_Scheduled_Candidate_When_Scheduling_Backlog_Then_CommandId_Is_Built_From_The_MusicCatalogId()
    {
        var env = NextMusicTracksRequestedForLookupHandlerTestEnvironment.WithScheduledCandidate();
        await env.RunSweep();
        env.CommandBus.SentCommands.OfType<IMusicCatalogLookupCommand>().Single().CommandId.Should().Be(CommandId.For("LookupMusicMetadata:mc_track_1"));
    }

    [Fact]
    public async Task Given_A_Source_Budget_Rejection_When_Scheduling_Backlog_Then_No_Command_Is_Returned()
    {
        var env = NextMusicTracksRequestedForLookupHandlerTestEnvironment.WithScheduledCandidate();
        env.SourceBudget.Reject(
            ProviderName.MusicBrainz,
            env.Now.AddMinutes(1),
            "MusicBrainz budget temporarily unavailable");

        await env.RunSweep();

        env.CommandBus.SentCommands.Should().BeEmpty();
    }
}
