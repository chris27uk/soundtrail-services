using FluentAssertions;
using Soundtrail.Contracts.Common;
using Soundtrail.Domain.Catalog;
using Soundtrail.Domain.Commands;
using Soundtrail.Services.Enrichment.Orchestrator.Shared.Search;
using Soundtrail.Domain.Discovery;
using Soundtrail.Domain.Enrichment.Commands;
using Soundtrail.Services.Enrichment.Orchestrator.Shared.Persistence;
using Soundtrail.Services.Tests.Unit.Enrichment.Infrastructure;

namespace Soundtrail.Services.Tests.Unit.Enrichment.Features.Scheduling;

public class CatalogSearchRequestedHandlerTests
{
    [Fact]
    public async Task Given_No_Active_Work_When_Handling_A_Schedulable_Request_Then_ShouldSchedule_Is_True()
    {
        var env = CatalogSearchRequestedHandlerTestEnvironment.WithNoExistingCandidates();
        env.Search.ResolveAs(MusicCatalogId.From("mc_track_1"));

        var result = await env.Handler.Handle(env.Request("rare unknown song", trustLevel: 1, riskScore: 10), CancellationToken.None);

        result.ShouldSchedule.Should().BeTrue();
        result.EstimatedRetryAfterSeconds.Should().Be(30);
        result.Reason.Should().Be("Planner queued lookup");
    }

    [Fact]
    public async Task Given_No_Active_Work_When_Handling_A_Schedulable_Request_Then_An_Active_Work_Lock_Is_Acquired()
    {
        var env = CatalogSearchRequestedHandlerTestEnvironment.WithNoExistingCandidates();
        env.Search.ResolveAs(MusicCatalogId.From("mc_track_1"));

        await env.Handler.Handle(env.Request("rare unknown song", trustLevel: 1, riskScore: 10), CancellationToken.None);

        env.ActiveWorkStore.Locks.Should().ContainSingle();
    }

    [Fact]
    public async Task Given_Active_Work_Already_Exists_When_Handling_A_Schedulable_Request_Then_No_Command_Is_Returned()
    {
        var env = CatalogSearchRequestedHandlerTestEnvironment.WithNoExistingCandidates();
        var musicCatalogId = MusicCatalogId.From("mc_track_1");
        env.Search.ResolveAs(musicCatalogId);
        await env.ActiveWorkStore.TryAcquireAsync(CommandId.For($"LookupMusicMetadata:{musicCatalogId.Value}"), DateTimeOffset.UtcNow.AddMinutes(5), CancellationToken.None);

        var result = await env.Handler.Handle(env.Request("rare unknown song", trustLevel: 1, riskScore: 10), CancellationToken.None);

        result.ShouldSchedule.Should().BeFalse();
        result.EstimatedRetryAfterSeconds.Should().Be(60);
        result.Reason.Should().Be("Planner deferred lookup");
    }

    [Fact]
    public async Task Given_Local_Search_Has_Isrc_When_Handling_A_Schedulable_Request_Then_Playback_References_Work_Is_Scheduled()
    {
        var env = CatalogSearchRequestedHandlerTestEnvironment.WithNoExistingCandidates();
        env.Search.ResolveAs(MusicCatalogId.From("mc_track_1"));
        env.LocalSearch.Seed(new LocalMusicTrackSearchResult(
            MusicCatalogId.From("mc_track_1"),
            "Song A",
            "Artist A",
            "Album A",
            "isrc-1",
            "mbid-1",
            123000,
            IsPlayable: false));

        var result = await env.Handler.Handle(env.Request("rare unknown song", trustLevel: 1, riskScore: 10), CancellationToken.None);

        env.CommandBus.SentCommands.Should().ContainSingle().Which.Should().BeOfType<LookupStreamingLocationsCommand>();
    }

    [Fact]
    public async Task Given_A_Schedulable_Request_When_Handled_Then_Provider_Budget_Is_Not_Checked_In_The_Orchestrator()
    {
        var env = CatalogSearchRequestedHandlerTestEnvironment.WithNoExistingCandidates();
        env.Search.ResolveAs(MusicCatalogId.From("mc_track_1"));

        var result = await env.Handler.Handle(env.Request("rare unknown song", trustLevel: 1, riskScore: 10), CancellationToken.None);

        result.ShouldSchedule.Should().BeTrue();
        env.CommandBus.SentCommands.Should().ContainSingle().Which.Should().BeOfType<LookupMusicMetadataCommand>();
    }

    [Fact]
    public async Task Given_An_Existing_Eligible_Candidate_When_Handling_A_Low_Risk_Request_Then_It_Is_Updated_And_Scheduled()
    {
        const string musicCatalogId = "mc_track_1";
        var env = CatalogSearchRequestedHandlerTestEnvironment.WithExistingEligibleCandidate(musicCatalogId);

        var result = await env.Handler.Handle(env.Request("rare unknown song", trustLevel: 2, riskScore: 15), CancellationToken.None);

        result.ShouldSchedule.Should().BeTrue();
        env.CommandBus.SentCommands.Should().ContainSingle();
        var sentCommand = env.CommandBus.SentCommands.Single().Should().BeOfType<LookupMusicMetadataCommand>().Subject;
        sentCommand.MusicCatalogId.Should().Be(MusicCatalogId.From(musicCatalogId));
        sentCommand.Priority.Should().Be(LookupPriorityBand.High);
        env.PotentialCatalogLookupWorks.Should().ContainSingle();
        env.PotentialCatalogLookupWorks[0].MusicCatalogId.Should().Be(MusicCatalogId.From(musicCatalogId));
        env.PotentialCatalogLookupWorks[0].RequestCount.Should().Be(3);
        env.PotentialCatalogLookupWorks[0].HighestTrustLevelSeen.Should().Be(2);
        env.PotentialCatalogLookupWorks[0].RiskScore.Should().Be(15);
        env.PotentialCatalogLookupWorks[0].Status.Should().Be(PotentialCatalogLookupWorkStatus.Pending);
    }

    [Fact]
    public async Task Given_An_Existing_Eligible_Candidate_With_Stronger_History_When_Handling_A_Request_Then_The_Stronger_Trust_And_Risk_Are_Preserved()
    {
        const string musicCatalogId = "mc_track_1";
        var env = CatalogSearchRequestedHandlerTestEnvironment.WithExistingCandidate(
            Candidates.ExistingCandidate(
                MusicCatalogId.From(musicCatalogId),
                requestCount: 2,
                highestTrustLevelSeen: 3,
                riskScore: 70,
                status: PotentialCatalogLookupWorkStatus.Pending,
                nextEligibleAt: null));

        await env.Handler.Handle(env.Request("rare unknown song", trustLevel: 1, riskScore: 15), CancellationToken.None);

        env.PotentialCatalogLookupWorks.Should().ContainSingle();
        env.PotentialCatalogLookupWorks[0].RequestCount.Should().Be(3);
        env.PotentialCatalogLookupWorks[0].HighestTrustLevelSeen.Should().Be(3);
        env.PotentialCatalogLookupWorks[0].RiskScore.Should().Be(70);
        env.PotentialCatalogLookupWorks[0].Status.Should().Be(PotentialCatalogLookupWorkStatus.Pending);
    }

    [Fact]
    public async Task Given_An_Existing_Eligible_Candidate_When_Handling_A_High_Risk_Request_Then_It_Is_Not_Scheduled_And_Remains_Pending()
    {
        const string musicCatalogId = "mc_track_1";
        var env = CatalogSearchRequestedHandlerTestEnvironment.WithExistingEligibleCandidate(musicCatalogId);

        var result = await env.Handler.Handle(env.Request("rare unknown song", trustLevel: 2, riskScore: 60), CancellationToken.None);

        result.ShouldSchedule.Should().BeFalse();
        env.PotentialCatalogLookupWorks.Should().ContainSingle();
        env.PotentialCatalogLookupWorks[0].RequestCount.Should().Be(3);
        env.PotentialCatalogLookupWorks[0].Status.Should().Be(PotentialCatalogLookupWorkStatus.Pending);
    }

    [Fact]
    public async Task Given_An_Existing_Candidate_That_Is_Not_Yet_Eligible_When_Handling_A_Request_Then_It_Is_Not_Scheduled_And_The_Request_Is_Recorded()
    {
        const string musicCatalogId = "mc_track_1";
        var occurredAt = new DateTimeOffset(2026, 5, 31, 12, 0, 0, TimeSpan.Zero);
        var env = CatalogSearchRequestedHandlerTestEnvironment.WithExistingNotYetEligibleCandidate(musicCatalogId);

        var result = await env.Handler.Handle(
            env.Request("rare unknown song", trustLevel: 1, riskScore: 0, occurredAt: occurredAt),
            CancellationToken.None);

        result.ShouldSchedule.Should().BeFalse();
        result.EstimatedRetryAfterSeconds.Should().Be(60);
        result.Reason.Should().Be("Planner deferred lookup");
        env.PotentialCatalogLookupWorks.Should().ContainSingle();
        env.PotentialCatalogLookupWorks[0].MusicCatalogId.Should().Be(MusicCatalogId.From(musicCatalogId));
        env.PotentialCatalogLookupWorks[0].RequestCount.Should().Be(2);
    }

    [Fact]
    public async Task Given_A_Request_That_Cannot_Be_Resolved_When_Handling_Then_No_Candidate_Is_Stored()
    {
        var env = CatalogSearchRequestedHandlerTestEnvironment.WithNoExistingCandidates();
        env.Search.Fails();

        await env.Handler.Handle(env.Request("rare unknown song", trustLevel: 0, riskScore: 100), CancellationToken.None);

        env.PotentialCatalogLookupWorks.Should().BeEmpty();
    }

    [Fact]
    public async Task Given_A_Resolved_Request_With_No_Previous_Candidate_When_Handling_A_Low_Risk_Request_Then_A_Pending_Candidate_And_Core_Trackings_Are_Stored()
    {
        var env = CatalogSearchRequestedHandlerTestEnvironment.WithNoExistingCandidates();
        env.Search.ResolveAs(MusicCatalogId.From("mc_track_1"));
        var occurredAt = new DateTimeOffset(2026, 5, 31, 12, 34, 56, TimeSpan.Zero);

        var result = await env.Handler.Handle(
            env.Request("rare unknown song", trustLevel: 1, riskScore: 10, occurredAt: occurredAt),
            CancellationToken.None);

        result.ShouldSchedule.Should().BeTrue();
        env.CommandBus.SentCommands.Should().ContainSingle();
        var sentCommand = env.CommandBus.SentCommands.Single().Should().BeOfType<LookupMusicMetadataCommand>().Subject;
        sentCommand.MusicCatalogId.Should().Be(MusicCatalogId.From("mc_track_1"));
        sentCommand.Priority.Should().Be(LookupPriorityBand.Low);
        sentCommand.CreatedAt.Should().Be(occurredAt);
        sentCommand.CommandId.Should().Be(CommandId.For("LookupMusicMetadata:mc_track_1"));
        sentCommand.CorrelationId.Value.Should().NotBeNullOrWhiteSpace();

        env.PotentialCatalogLookupWorks.Should().ContainSingle();
        env.PotentialCatalogLookupWorks[0].MusicCatalogId.Should().Be(MusicCatalogId.From("mc_track_1"));
        env.PotentialCatalogLookupWorks[0].RequestCount.Should().Be(1);
        env.PotentialCatalogLookupWorks[0].HighestTrustLevelSeen.Should().Be(1);
        env.PotentialCatalogLookupWorks[0].RiskScore.Should().Be(10);
        env.PotentialCatalogLookupWorks[0].Status.Should().Be(PotentialCatalogLookupWorkStatus.Pending);
        env.PotentialCatalogLookupWorks[0].NextEligibleAt.Should().BeNull();

        env.CatalogSearchTrackings.Select(x => x.Criteria.Value).Should().BeEquivalentTo(
            "search:track:rare unknown song",
            "track:mc_track_1");
        env.CatalogSearchTrackings.Should().OnlyContain(x => x.MusicCatalogId == MusicCatalogId.From("mc_track_1"));
    }

    [Fact]
    public async Task Given_A_Resolved_Request_With_Known_Hierarchy_When_Handling_Then_Artist_And_Album_Trackings_Are_Stored()
    {
        var env = CatalogSearchRequestedHandlerTestEnvironment.WithNoExistingCandidates();
        env.Search.ResolveAs(MusicCatalogId.From("mc_track_1"));
        env.LocalSearch.Seed(new LocalMusicTrackSearchResult(
            MusicCatalogId.From("mc_track_1"),
            "Song A",
            "Artist A",
            "Album A",
            null,
            null,
            null,
            IsPlayable: false,
            ArtistId.From("artist_a"),
            AlbumId.From("album_a")));

        await env.Handler.Handle(env.Request("rare unknown song", trustLevel: 1, riskScore: 10), CancellationToken.None);

        env.CatalogSearchTrackings.Select(x => x.Criteria.Value).Should().BeEquivalentTo(
            "search:track:rare unknown song",
            "track:mc_track_1",
            "artist:artist_a",
            "album:album_a");
    }

    [Theory]
    [InlineData(60, PotentialCatalogLookupWorkStatus.Pending)]
    [InlineData(90, PotentialCatalogLookupWorkStatus.Ignored)]
    public async Task Given_A_Resolved_Request_With_No_Previous_Candidate_When_Handling_A_High_Or_Blocked_Risk_Request_Then_The_Candidate_Is_Stored_With_The_Expected_Status(
        int riskScore,
        PotentialCatalogLookupWorkStatus expectedStatus)
    {
        var env = CatalogSearchRequestedHandlerTestEnvironment.WithNoExistingCandidates();
        env.Search.ResolveAs(MusicCatalogId.From("mc_track_1"));

        var result = await env.Handler.Handle(
            env.Request("rare unknown song", trustLevel: 1, riskScore: riskScore),
            CancellationToken.None);

        result.ShouldSchedule.Should().BeFalse();
        result.EstimatedRetryAfterSeconds.Should().Be(60);
        result.Reason.Should().Be("Planner deferred lookup");
        env.PotentialCatalogLookupWorks.Should().ContainSingle();
        env.PotentialCatalogLookupWorks[0].RiskScore.Should().Be(riskScore);
        env.PotentialCatalogLookupWorks[0].Status.Should().Be(expectedStatus);
    }

    [Fact]
    public async Task Given_A_Single_Exact_Query_Match_When_Handling_A_Request_With_A_Close_Alternative_Then_The_Exact_Query_Match_Is_Scheduled()
    {
        var env = CatalogSearchRequestedHandlerTestEnvironment.WithNoExistingCandidates();
        env.Search.ReturnMatches(
            new MusicCatalogMatch(
                MusicCatalogId.From("mc_track_1"),
                0.90m,
                new MusicCatalogMatchEvidence(
                    false,
                    "rare unknown song",
                    "test artist",
                    null,
                    null,
                    null,
                    null)),
            new MusicCatalogMatch(
                MusicCatalogId.From("mc_track_2"),
                0.85m,
                new MusicCatalogMatchEvidence(
                    false,
                    "rare unknown song",
                    "other artist",
                    null,
                    null,
                    null,
                    null)));

        var result = await env.Handler.Handle(
            env.Request("rare unknown song test artist", trustLevel: 1, riskScore: 10),
            CancellationToken.None);

        result.ShouldSchedule.Should().BeTrue();
        env.CommandBus.SentCommands.Should().ContainSingle();
        env.CommandBus.SentCommands.Single().Should().BeOfType<LookupMusicMetadataCommand>().Which.MusicCatalogId.Should().Be(MusicCatalogId.From("mc_track_1"));
    }

    [Fact]
    public async Task Given_Multiple_Exact_Identity_Matches_When_Handling_A_Request_With_A_Local_Release_Date_Then_The_Matching_Release_Date_Is_Selected()
    {
        var env = CatalogSearchRequestedHandlerTestEnvironment.WithNoExistingCandidates();
        env.LocalSearch.Seed(new LocalMusicTrackSearchResult(
            MusicCatalogId.From("mc_track_criteria"),
            "Rare Unknown Song",
            "Test Artist",
            "Rare Album",
            null,
            null,
            null,
            IsPlayable: false,
            ReleaseDate: new DateOnly(2004, 6, 7)));
        env.Search.ReturnMatches(
            new MusicCatalogMatch(
                MusicCatalogId.From("mc_track_1"),
                0.99m,
                new MusicCatalogMatchEvidence(true, null, null, null, null, "mbid-1", new DateOnly(2004, 6, 7))),
            new MusicCatalogMatch(
                MusicCatalogId.From("mc_track_2"),
                1.00m,
                new MusicCatalogMatchEvidence(true, null, null, null, null, "mbid-2", new DateOnly(2005, 6, 7))));

        var result = await env.Handler.Handle(
            env.Request("rare unknown song", trustLevel: 1, riskScore: 10) with
            {
                Criteria = CatalogSearchCriteria.Track(TrackId.From("mc_track_criteria"))
            },
            CancellationToken.None);

        result.ShouldSchedule.Should().BeTrue();
        env.CommandBus.SentCommands.Should().ContainSingle();
        env.CommandBus.SentCommands.Single().Should().BeOfType<LookupMusicMetadataCommand>().Which.MusicCatalogId.Should().Be(MusicCatalogId.From("mc_track_1"));
    }

    [Fact]
    public async Task Given_A_Resolved_Request_When_Handling_A_Request_At_The_Minimum_Accepted_Score_Then_It_Is_Scheduled()
    {
        var env = CatalogSearchRequestedHandlerTestEnvironment.WithNoExistingCandidates();
        env.Search.ReturnMatches(new MusicCatalogMatch(MusicCatalogId.From("mc_track_1"), 0.80m));

        var result = await env.Handler.Handle(
            env.Request("rare unknown song", trustLevel: 1, riskScore: 10),
            CancellationToken.None);

        result.ShouldSchedule.Should().BeTrue();
        env.CommandBus.SentCommands.Should().ContainSingle();
        env.CommandBus.SentCommands.Single().Should().BeOfType<LookupMusicMetadataCommand>().Which.MusicCatalogId.Should().Be(MusicCatalogId.From("mc_track_1"));
    }

    [Fact]
    public async Task Given_A_Resolved_Request_When_Handling_A_Request_At_The_Minimum_Winning_Margin_Then_It_Is_Scheduled()
    {
        var env = CatalogSearchRequestedHandlerTestEnvironment.WithNoExistingCandidates();
        env.Search.ReturnMatches(
            new MusicCatalogMatch(MusicCatalogId.From("mc_track_1"), 0.90m),
            new MusicCatalogMatch(MusicCatalogId.From("mc_track_2"), 0.80m));

        var result = await env.Handler.Handle(
            env.Request("rare unknown song", trustLevel: 1, riskScore: 10),
            CancellationToken.None);

        result.ShouldSchedule.Should().BeTrue();
        env.CommandBus.SentCommands.Should().ContainSingle();
        env.CommandBus.SentCommands.Single().Should().BeOfType<LookupMusicMetadataCommand>().Which.MusicCatalogId.Should().Be(MusicCatalogId.From("mc_track_1"));
    }
}
