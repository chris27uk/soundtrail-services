using FluentAssertions;
using Soundtrail.Contracts.Common;
using Soundtrail.Domain.Commands;
using Soundtrail.Domain.Discovery;
using Soundtrail.Services.Tests.Unit.Enrichment.Infrastructure;

namespace Soundtrail.Services.Tests.Unit.Enrichment.Features.Scheduling;

public class LookupMusicRequestHandlerTests
{
    [Fact]
    public async Task Given_No_Active_Work_When_Handling_A_Schedulable_Request_Then_ShouldSchedule_Is_True()
    {
        var env = LookupMusicRequestHandlerTestEnvironment.WithNoExistingCandidates();
        env.Search.ResolveAs(MusicCatalogId.From("mc_track_1"));

        var result = await env.Handler.Handle(env.Request("rare unknown song", trustLevel: 1, riskScore: 10), CancellationToken.None);

        result.ShouldSchedule.Should().BeTrue();
    }

    [Fact]
    public async Task Given_No_Active_Work_When_Handling_A_Schedulable_Request_Then_An_Active_Work_Lock_Is_Acquired()
    {
        var env = LookupMusicRequestHandlerTestEnvironment.WithNoExistingCandidates();
        env.Search.ResolveAs(MusicCatalogId.From("mc_track_1"));

        await env.Handler.Handle(env.Request("rare unknown song", trustLevel: 1, riskScore: 10), CancellationToken.None);

        env.ActiveWorkStore.Locks.Should().ContainSingle();
    }

    [Fact]
    public async Task Given_Active_Work_Already_Exists_When_Handling_A_Schedulable_Request_Then_No_Command_Is_Returned()
    {
        var env = LookupMusicRequestHandlerTestEnvironment.WithNoExistingCandidates();
        var musicCatalogId = MusicCatalogId.From("mc_track_1");
        env.Search.ResolveAs(musicCatalogId);
        await env.ActiveWorkStore.TryAcquireAsync(CommandId.For($"LookupCanonicalMusicMetadata:{musicCatalogId.Value}"), DateTimeOffset.UtcNow.AddMinutes(5), CancellationToken.None);

        var result = await env.Handler.Handle(env.Request("rare unknown song", trustLevel: 1, riskScore: 10), CancellationToken.None);

        result.ShouldSchedule.Should().BeFalse();
    }

    [Fact]
    public async Task Given_Local_Search_Has_Isrc_When_Handling_A_Schedulable_Request_Then_Playback_References_Work_Is_Scheduled()
    {
        var env = LookupMusicRequestHandlerTestEnvironment.WithNoExistingCandidates();
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
}
