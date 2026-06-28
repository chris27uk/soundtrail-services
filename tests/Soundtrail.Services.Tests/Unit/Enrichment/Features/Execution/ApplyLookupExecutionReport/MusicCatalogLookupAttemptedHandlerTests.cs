using FluentAssertions;
using Soundtrail.Contracts.Common;
using Soundtrail.Domain.Discovery.Events;
using Soundtrail.Domain.Enrichment.Commands;
using Soundtrail.Domain.Enrichment.Responses;
using Soundtrail.Services.Tests.Unit.Enrichment.Infrastructure;

namespace Soundtrail.Services.Tests.Unit.Enrichment.Features.Execution.ApplyLookupExecutionReport;

public sealed class MusicCatalogLookupAttemptedHandlerTests
{
    [Fact]
    public async Task Given_A_Lookup_Attempt_When_Handled_Then_Catalog_And_Discovery_Follow_Up_Commands_Are_Sent()
    {
        var env = MusicCatalogLookupAttemptedHandlerTestEnvironment.Create();
        var attempted = new MusicCatalogLookupAttempted(
            CommandId.For("LookupTrackMetadata:mc_track_1"),
            MusicCatalogId.From("mc_track_1"),
            LookupSource.MusicBrainz,
            LookupPriorityBand.High,
            env.Now,
            CorrelationId.From("corr-1"),
            MusicCatalogLookupOutcome.Completed(),
            null);

        await env.Handler.Handle(attempted, CancellationToken.None);

        env.Bus.SentCommands.Should().ContainSingle(x => x is ApplyMusicCatalogLookupAttemptedToCatalogCommand);
        env.Bus.SentCommands.Should().ContainSingle(x => x is ApplyMusicCatalogLookupAttemptedToDiscoveryCommand);
    }

    [Fact]
    public async Task Given_A_Deferred_Report_When_Handled_Then_All_Tracked_Discoveries_Are_Deferred()
    {
        var env = MusicCatalogLookupAttemptedHandlerTestEnvironment.Create();

        await env.DiscoveryHandler.Handle(
            new ApplyMusicCatalogLookupAttemptedToDiscoveryCommand(new MusicCatalogLookupAttempted(
                CommandId.For("LookupTrackMetadata:mc_track_1"),
                MusicCatalogId.From("mc_track_1"),
                LookupSource.MusicBrainz,
                LookupPriorityBand.High,
                env.Now,
                CorrelationId.From("corr-1"),
                MusicCatalogLookupOutcome.Deferred(
                    "MusicBrainz budget temporarily unavailable",
                    env.Now.AddMinutes(1),
                    60),
                null)),
            CancellationToken.None);

        env.StoredEvents("search:track:rare unknown song").Last().Should().BeOfType<DiscoveryDeferred>();
    }

    [Fact]
    public async Task Given_A_Failed_Report_When_Handled_Then_All_Tracked_Discoveries_Are_Failed()
    {
        var env = MusicCatalogLookupAttemptedHandlerTestEnvironment.Create();

        await env.DiscoveryHandler.Handle(
            new ApplyMusicCatalogLookupAttemptedToDiscoveryCommand(new MusicCatalogLookupAttempted(
                CommandId.For("LookupTrackMetadata:mc_track_1"),
                MusicCatalogId.From("mc_track_1"),
                LookupSource.MusicBrainz,
                LookupPriorityBand.High,
                env.Now,
                CorrelationId.From("corr-1"),
                MusicCatalogLookupOutcome.Failed("Lookup failed"),
                null)),
            CancellationToken.None);

        env.StoredEvents("search:track:rare unknown song").Last().Should().BeOfType<DiscoveryFailed>();
    }

    [Fact]
    public async Task Given_A_Failed_Report_When_Handled_Then_All_Tracked_Discoveries_Are_Started_Before_Failing()
    {
        var env = MusicCatalogLookupAttemptedHandlerTestEnvironment.Create();
        var before = env.StoredEvents("search:track:rare unknown song").Count;

        await env.DiscoveryHandler.Handle(
            new ApplyMusicCatalogLookupAttemptedToDiscoveryCommand(new MusicCatalogLookupAttempted(
                CommandId.For("LookupTrackMetadata:mc_track_1"),
                MusicCatalogId.From("mc_track_1"),
                LookupSource.MusicBrainz,
                LookupPriorityBand.High,
                env.Now,
                CorrelationId.From("corr-1"),
                MusicCatalogLookupOutcome.Failed("Lookup failed"),
                null)),
            CancellationToken.None);

        env.StoredEvents("search:track:rare unknown song").Should().HaveCount(before + 2);
        env.StoredEvents("search:track:rare unknown song")[^2].Should().BeOfType<DiscoveryStarted>();
        env.StoredEvents("search:track:rare unknown song")[^1].Should().BeOfType<DiscoveryFailed>();
    }

    [Fact]
    public async Task Given_A_Completed_Report_When_Handled_Then_Discovery_Is_Marked_As_Started_When_Not_Already_Started()
    {
        var env = MusicCatalogLookupAttemptedHandlerTestEnvironment.Create();
        var before = env.StoredEvents("search:track:rare unknown song").Count;

        await env.DiscoveryHandler.Handle(
            new ApplyMusicCatalogLookupAttemptedToDiscoveryCommand(new MusicCatalogLookupAttempted(
                CommandId.For("LookupTrackMetadata:mc_track_1"),
                MusicCatalogId.From("mc_track_1"),
                LookupSource.MusicBrainz,
                LookupPriorityBand.High,
                env.Now,
                CorrelationId.From("corr-1"),
                MusicCatalogLookupOutcome.Completed(),
                null)),
            CancellationToken.None);

        env.StoredEvents("search:track:rare unknown song").Should().HaveCount(before + 1);
        env.StoredEvents("search:track:rare unknown song").Last().Should().BeOfType<DiscoveryStarted>();
    }
}
