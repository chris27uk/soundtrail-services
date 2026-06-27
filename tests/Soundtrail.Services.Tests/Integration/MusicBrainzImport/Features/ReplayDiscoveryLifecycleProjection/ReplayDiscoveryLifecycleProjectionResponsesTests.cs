using FluentAssertions;
using Soundtrail.Contracts.Common;
using Soundtrail.Domain.Discovery;
using Soundtrail.Domain.Discovery.Commands;
using Soundtrail.Services.Tests.Integration.Api.Infrastructure;

namespace Soundtrail.Services.Tests.Integration.MusicBrainzImport.Features.ReplayDiscoveryLifecycleProjection;

[Collection(RavenEmbeddedCollection.Name)]
public sealed class ReplayDiscoveryLifecycleProjectionResponsesTests
{
    [Theory]
    [MemberData(nameof(ReplayDiscoveryLifecycleProjectionModes.All), MemberType = typeof(ReplayDiscoveryLifecycleProjectionModes))]
    public async Task Given_A_Stale_Discovery_Status_When_Replay_All_Is_Run_Then_The_Status_Is_Rebuilt_From_Stored_Events(
        ReplayDiscoveryLifecycleProjectionMode mode)
    {
        await using var env = await ReplayDiscoveryLifecycleProjectionTestEnvironment.CreateAsync(mode);
        var criteria = MusicSearchCriteria.ByQuery("rare unknown song", SearchTypesFilter.Tracks);

        await env.Handler.Handle(
            new ReplayDiscoveryLifecycleProjectionBatchCommand(),
            CancellationToken.None);

        var status = await env.LoadStatusAsync(criteria);
        status.Should().NotBeNull();
        status!.Status.Should().Be(CatalogSearchLifecycleStatus.Planned.ToString());
        status.Priority.Should().Be(LookupPriorityBand.High.ToString());
        status.WillBeLookedUp.Should().BeTrue();
        status.EstimatedRetryAfterSeconds.Should().Be(30);
        status.Reason.Should().Be("Planner queued lookup");

        var checkpointVersion = await env.LoadCheckpointVersionAsync(criteria);
        checkpointVersion.Should().Be(2);
    }

    [Fact]
    public async Task Given_An_Existing_Discovery_Status_Document_When_Replay_All_Is_Run_Then_The_Document_Is_Updated_In_Place()
    {
        await using var env = await ReplayDiscoveryLifecycleProjectionTestEnvironment.CreateAsync(
            ReplayDiscoveryLifecycleProjectionMode.RavenEmbedded);

        await env.Handler.Handle(
            new ReplayDiscoveryLifecycleProjectionBatchCommand(),
            CancellationToken.None);

        var statusDocumentCount = await env.CountStatusDocumentsAsync();

        statusDocumentCount.Should().Be(1);
    }
}
