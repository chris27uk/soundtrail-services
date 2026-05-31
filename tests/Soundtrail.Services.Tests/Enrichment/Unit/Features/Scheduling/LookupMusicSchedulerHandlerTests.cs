using FluentAssertions;
using Soundtrail.Services.Enrichment.Features.Scheduling.Models;
using Soundtrail.Services.Features.Search.Models;

namespace Soundtrail.Services.Tests.Enrichment.Unit.Features.Scheduling;

public sealed class LookupMusicSchedulerHandlerTests
{
    [Fact]
    public async Task Given_A_Resolved_Request_When_Handled_Then_A_Ranked_Music_Candidate_Is_Created_And_A_Command_Is_Queued()
    {
        var env = LookupMusicSchedulerHandlerTestEnvironment.Empty();
        env.ResolutionPort.ResolveAs(MusicCatalogId.From("mc_track_1"));

        await env.Handler.Handle(env.Request("rare unknown song", trustLevel: 1, riskScore: 10));

        env.RankedMusicCandidates.Should().ContainSingle();
        env.RankedMusicCandidates[0].MusicCatalogId.Value.Should().Be("mc_track_1");
        env.RankedMusicCandidates[0].Query.Value.Should().Be("rare unknown song");
        env.RankedMusicCandidates[0].RequestCount.Should().Be(1);
        env.RankedMusicCandidates[0].HighestTrustLevelSeen.Should().Be(1);
        env.Commands.Should().ContainSingle();
        env.Commands[0].MusicCatalogId.Value.Should().Be("mc_track_1");
        env.Commands[0].Query.Value.Should().Be("rare unknown song");
        env.DeadLetters.Should().BeEmpty();
    }

    [Fact]
    public async Task Given_An_Existing_Candidate_For_The_Same_MusicCatalogId_When_Handled_Then_The_Candidate_Is_Updated_And_A_Command_Is_Queued()
    {
        var musicCatalogId = MusicCatalogId.From("mc_track_1");
        var existingCandidate = new RankedMusicCandidate(
            RankedMusicCandidateId: QueryId.New(),
            MusicCatalogId: musicCatalogId,
            Query: NormalizedSearchQuery.FromText("rare unknown song"),
            RequestCount: 2,
            HighestTrustLevelSeen: 0,
            RiskScore: 5,
            Status: RankedMusicCandidateStatus.Pending,
            FirstSeenAt: new DateTimeOffset(2026, 5, 31, 10, 0, 0, TimeSpan.Zero),
            LastSeenAt: new DateTimeOffset(2026, 5, 31, 10, 30, 0, TimeSpan.Zero),
            NextEligibleAt: null);
        var env = LookupMusicSchedulerHandlerTestEnvironment.WithExistingCandidate(existingCandidate);
        env.ResolutionPort.ResolveAs(musicCatalogId);

        await env.Handler.Handle(env.Request("rare unknown song", trustLevel: 2, riskScore: 15));

        env.RankedMusicCandidates.Should().ContainSingle();
        env.RankedMusicCandidates[0].RankedMusicCandidateId.Should().Be(existingCandidate.RankedMusicCandidateId);
        env.RankedMusicCandidates[0].MusicCatalogId.Should().Be(musicCatalogId);
        env.RankedMusicCandidates[0].RequestCount.Should().Be(3);
        env.RankedMusicCandidates[0].HighestTrustLevelSeen.Should().Be(2);
        env.RankedMusicCandidates[0].RiskScore.Should().Be(15);
        env.Commands.Should().ContainSingle();
        env.Commands[0].MusicCatalogId.Should().Be(musicCatalogId);
    }

    [Fact]
    public async Task Given_A_Request_That_Cannot_Be_Resolved_When_Handled_Then_It_Is_DeadLettered_And_No_Candidate_Is_Stored()
    {
        var env = LookupMusicSchedulerHandlerTestEnvironment.Empty();
        env.ResolutionPort.Fails();

        await env.Handler.Handle(env.Request("rare unknown song", trustLevel: 0, riskScore: 100));

        env.RankedMusicCandidates.Should().BeEmpty();
        env.Commands.Should().BeEmpty();
        env.DeadLetters.Should().ContainSingle();
        env.DeadLetters[0].Reason.Should().Be("resolution_failed");
    }

    [Fact]
    public async Task Given_A_Resolved_Request_That_Is_Not_Yet_Eligible_When_Handled_Then_The_Candidate_Is_Updated_But_No_Command_Is_Queued()
    {
        var musicCatalogId = MusicCatalogId.From("mc_track_1");
        var env = LookupMusicSchedulerHandlerTestEnvironment.WithExistingCandidate(
            new RankedMusicCandidate(
                RankedMusicCandidateId: QueryId.New(),
                MusicCatalogId: musicCatalogId,
                Query: NormalizedSearchQuery.FromText("rare unknown song"),
                RequestCount: 1,
                HighestTrustLevelSeen: 0,
                RiskScore: 0,
                Status: RankedMusicCandidateStatus.Pending,
                FirstSeenAt: new DateTimeOffset(2026, 5, 31, 10, 0, 0, TimeSpan.Zero),
                LastSeenAt: new DateTimeOffset(2026, 5, 31, 10, 0, 0, TimeSpan.Zero),
                NextEligibleAt: new DateTimeOffset(2026, 5, 31, 13, 0, 0, TimeSpan.Zero)));
        env.ResolutionPort.ResolveAs(musicCatalogId);

        await env.Handler.Handle(env.Request(
            "rare unknown song",
            trustLevel: 1,
            riskScore: 0,
            occurredAt: new DateTimeOffset(2026, 5, 31, 12, 0, 0, TimeSpan.Zero)));

        env.RankedMusicCandidates.Should().ContainSingle();
        env.RankedMusicCandidates[0].RequestCount.Should().Be(2);
        env.Commands.Should().BeEmpty();
    }
}