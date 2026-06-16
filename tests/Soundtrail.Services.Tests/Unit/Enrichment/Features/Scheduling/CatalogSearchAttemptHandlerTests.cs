using FluentAssertions;
using Soundtrail.Contracts.Common;
using Soundtrail.Domain.Commands;
using Soundtrail.Domain.Discovery;
using Soundtrail.Services.Tests.Unit.Enrichment.Infrastructure;

namespace Soundtrail.Services.Tests.Unit.Enrichment.Features.Scheduling;

public class CatalogSearchAttemptHandlerTests
{
    [Fact]
    public async Task Given_No_Active_Work_When_Handling_A_Schedulable_Request_Then_ShouldSchedule_Is_True()
    {
        var env = CatalogSearchAttemptHandlerTestEnvironment.WithNoExistingCandidates();
        env.Search.ResolveAs(MusicCatalogId.From("mc_track_1"));

        var result = await env.Handler.Handle(env.Request("rare unknown song", trustLevel: 1, riskScore: 10), CancellationToken.None);

        result.ShouldSchedule.Should().BeTrue();
        result.EstimatedRetryAfterSeconds.Should().Be(30);
        result.Reason.Should().Be("Planner queued lookup");
    }

    [Fact]
    public async Task Given_No_Active_Work_When_Handling_A_Schedulable_Request_Then_An_Active_Work_Lock_Is_Acquired()
    {
        var env = CatalogSearchAttemptHandlerTestEnvironment.WithNoExistingCandidates();
        env.Search.ResolveAs(MusicCatalogId.From("mc_track_1"));

        await env.Handler.Handle(env.Request("rare unknown song", trustLevel: 1, riskScore: 10), CancellationToken.None);

        env.ActiveWorkStore.Locks.Should().ContainSingle();
    }

    [Fact]
    public async Task Given_Active_Work_Already_Exists_When_Handling_A_Schedulable_Request_Then_No_Command_Is_Returned()
    {
        var env = CatalogSearchAttemptHandlerTestEnvironment.WithNoExistingCandidates();
        var musicCatalogId = MusicCatalogId.From("mc_track_1");
        env.Search.ResolveAs(musicCatalogId);
        await env.ActiveWorkStore.TryAcquireAsync(CommandId.For($"LookupCanonicalMusicMetadata:{musicCatalogId.Value}"), DateTimeOffset.UtcNow.AddMinutes(5), CancellationToken.None);

        var result = await env.Handler.Handle(env.Request("rare unknown song", trustLevel: 1, riskScore: 10), CancellationToken.None);

        result.ShouldSchedule.Should().BeFalse();
        result.EstimatedRetryAfterSeconds.Should().Be(60);
        result.Reason.Should().Be("Planner deferred lookup");
    }

    [Fact]
    public async Task Given_Local_Search_Has_Isrc_When_Handling_A_Schedulable_Request_Then_Playback_References_Work_Is_Scheduled()
    {
        var env = CatalogSearchAttemptHandlerTestEnvironment.WithNoExistingCandidates();
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

        result.Commands.Should().ContainSingle().Which.Should().BeOfType<ResolvePlaybackReferencesCommand>();
    }

    [Fact]
    public async Task Given_A_MusicBrainz_Budget_Rejection_When_Handling_A_Schedulable_Request_Then_No_Command_Is_Returned()
    {
        var env = CatalogSearchAttemptHandlerTestEnvironment.WithNoExistingCandidates();
        env.Search.ResolveAs(MusicCatalogId.From("mc_track_1"));
        env.SourceBudget.Reject(
            ProviderName.MusicBrainz,
            new DateTimeOffset(2026, 5, 31, 12, 1, 0, TimeSpan.Zero),
            "MusicBrainz budget temporarily unavailable");

        var result = await env.Handler.Handle(env.Request("rare unknown song", trustLevel: 1, riskScore: 10), CancellationToken.None);

        result.ShouldSchedule.Should().BeFalse();
        result.Reason.Should().Be("MusicBrainz budget temporarily unavailable");
        result.EstimatedRetryAfterSeconds.Should().Be(60);
    }
}
