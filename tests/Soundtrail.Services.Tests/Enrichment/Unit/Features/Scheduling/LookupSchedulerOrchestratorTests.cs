using FluentAssertions;
using Soundtrail.Services.Enrichment.Features.Scheduling;
using Soundtrail.Services.Enrichment.Features.Scheduling.Models;
using Soundtrail.Services.Tests.Enrichment.Unit.Infrastructure;

namespace Soundtrail.Services.Tests.Enrichment.Unit.Features.Scheduling;

public class LookupSchedulerOrchestratorTests
{
    [Fact]
    public async Task Given_No_Active_Work_When_Handling_A_Schedulable_Request_Then_Command_Is_Queued()
    {
        var env = LookupMusicSchedulerHandlerTestEnvironment.WithNoExistingCandidates();
        env.Search.ResolveAs(MusicCatalogId.From("mc_track_1"));
        var activeWorkStore = new ActiveLookupWorkStoreFake();
        var commandQueue = new LookupMusicCommandQueueFake();
        var orchestrator = new LookupSchedulerOrchestrator(env.Handler, activeWorkStore, commandQueue);

        var command = await orchestrator.HandleAsync(env.Request("rare unknown song", trustLevel: 1, riskScore: 10));

        command.Should().NotBeNull();
        commandQueue.Commands.Should().ContainSingle();
        activeWorkStore.Reservations.Should().ContainSingle();
    }

    [Fact]
    public async Task Given_Active_Work_Already_Exists_When_Handling_A_Schedulable_Request_Then_No_Command_Is_Queued()
    {
        var env = LookupMusicSchedulerHandlerTestEnvironment.WithNoExistingCandidates();
        var musicCatalogId = MusicCatalogId.From("mc_track_1");
        env.Search.ResolveAs(musicCatalogId);
        var activeWorkStore = new ActiveLookupWorkStoreFake();
        await activeWorkStore.TryReserveAsync(musicCatalogId, "existing", DateTimeOffset.UtcNow.AddMinutes(5), CancellationToken.None);
        var commandQueue = new LookupMusicCommandQueueFake();
        var orchestrator = new LookupSchedulerOrchestrator(env.Handler, activeWorkStore, commandQueue);

        var command = await orchestrator.HandleAsync(env.Request("rare unknown song", trustLevel: 1, riskScore: 10));

        command.Should().BeNull();
        commandQueue.Commands.Should().BeEmpty();
    }
}
