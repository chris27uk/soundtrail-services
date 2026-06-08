using FluentAssertions;
using Soundtrail.Contracts.Common;
using Soundtrail.Services.Tests.Unit.Enrichment.Infrastructure;

namespace Soundtrail.Services.Tests.Unit.Enrichment.Features.Scheduling;

public class LookupMusicRequestHandlerTests
{
    [Fact]
    public async Task Given_No_Active_Work_When_Handling_A_Schedulable_Request_Then_A_Command_Is_Returned()
    {
        var env = LookupMusicRequestHandlerTestEnvironment.WithNoExistingCandidates();
        env.Search.ResolveAs(MusicCatalogId.From("mc_track_1"));

        var result = await env.Handler.Handle(env.Request("rare unknown song", trustLevel: 1, riskScore: 10), CancellationToken.None);

        result.ShouldSchedule.Should().BeTrue();
        env.ActiveWorkStore.Locks.Should().ContainSingle();
    }

    [Fact]
    public async Task Given_Active_Work_Already_Exists_When_Handling_A_Schedulable_Request_Then_No_Command_Is_Returned()
    {
        var env = LookupMusicRequestHandlerTestEnvironment.WithNoExistingCandidates();
        var musicCatalogId = MusicCatalogId.From("mc_track_1");
        env.Search.ResolveAs(musicCatalogId);
        await env.ActiveWorkStore.TryAcquireAsync(CommandId.For(musicCatalogId.Value), DateTimeOffset.UtcNow.AddMinutes(5), CancellationToken.None);

        var result = await env.Handler.Handle(env.Request("rare unknown song", trustLevel: 1, riskScore: 10), CancellationToken.None);

        result.ShouldSchedule.Should().BeFalse();
    }
}
