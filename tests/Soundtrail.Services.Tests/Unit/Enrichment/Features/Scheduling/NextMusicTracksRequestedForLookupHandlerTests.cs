using FluentAssertions;
using Soundtrail.Domain.Discovery.Commands;
using Soundtrail.Services.Tests.Unit.Enrichment.Infrastructure;

namespace Soundtrail.Services.Tests.Unit.Enrichment.Features.Scheduling;

public sealed class NextMusicTracksRequestedForLookupHandlerTests
{
    [Fact]
    public async Task Given_Eligible_Candidates_When_Sweep_Runs_Then_Assess_Commands_Are_Sent()
    {
        var env = NextMusicTracksRequestedForLookupHandlerTestEnvironment.WithHighAndLowPriorityEligibleCandidates();

        await env.RunSweep();

        env.CommandBus.SentCommands.Should().HaveCount(2);
        env.CommandBus.SentCommands.Should().OnlyContain(x => x is AssessMusicCatalogItemCommand);
    }

    [Fact]
    public async Task Given_More_Eligible_Candidates_Than_The_Batch_Size_When_Sweep_Runs_Then_Only_The_Top_Batch_Is_Sent()
    {
        var env = NextMusicTracksRequestedForLookupHandlerTestEnvironment.WithMoreEligibleCandidatesThanTheBatchSize();

        await env.RunSweep(take: 2);

        env.CommandBus.SentCommands.Should().HaveCount(2);
    }

    [Fact]
    public async Task Given_A_Not_Yet_Eligible_Candidate_When_Sweep_Runs_Then_No_Assess_Command_Is_Sent()
    {
        var env = NextMusicTracksRequestedForLookupHandlerTestEnvironment.WithNotYetEligibleCandidate();

        await env.RunSweep();

        env.CommandBus.SentCommands.Should().BeEmpty();
    }
}
