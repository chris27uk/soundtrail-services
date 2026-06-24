using FluentAssertions;
using Soundtrail.Contracts.Common;
using Soundtrail.Domain.Discovery;
using Soundtrail.Domain.Events;
using Soundtrail.Domain.Responses;
using Soundtrail.Services.Tests.Unit.Enrichment.Infrastructure;

namespace Soundtrail.Services.Tests.Unit.Enrichment.Features.Execution.ApplyLookupExecutionReport;

public sealed class MusicCatalogLookupAttemptedHandlerTests
{
    [Fact]
    public async Task Given_A_Deferred_Report_When_Handled_Then_All_Tracked_Discoveries_Are_Deferred()
    {
        var env = MusicCatalogLookupAttemptedHandlerTestEnvironment.Create();

        await env.Handler.Handle(
            new MusicCatalogLookupAttempted(
                CommandId.For("LookupMusicMetadata:mc_track_1"),
                MusicCatalogId.From("mc_track_1"),
                ProviderName.MusicBrainz,
                LookupPriorityBand.High,
                env.Now,
                CorrelationId.From("corr-1"),
                MusicCatalogLookupOutcome.Deferred(
                    "MusicBrainz budget temporarily unavailable",
                    env.Now.AddMinutes(1),
                    60),
                null),
            CancellationToken.None);

        env.StoredEvents("search:track:rare unknown song").Last().Should().BeOfType<DiscoveryDeferred>();
        env.StoredEvents("artist:artist_1").Last().Should().BeOfType<DiscoveryDeferred>();
    }

    [Fact]
    public async Task Given_A_Failed_Report_When_Handled_Then_All_Tracked_Discoveries_Are_Failed()
    {
        var env = MusicCatalogLookupAttemptedHandlerTestEnvironment.Create();

        await env.Handler.Handle(
            new MusicCatalogLookupAttempted(
                CommandId.For("LookupMusicMetadata:mc_track_1"),
                MusicCatalogId.From("mc_track_1"),
                ProviderName.MusicBrainz,
                LookupPriorityBand.High,
                env.Now,
                CorrelationId.From("corr-1"),
                MusicCatalogLookupOutcome.Failed("Lookup failed"),
                null),
            CancellationToken.None);

        env.StoredEvents("search:track:rare unknown song").Last().Should().BeOfType<DiscoveryFailed>();
        env.StoredEvents("artist:artist_1").Last().Should().BeOfType<DiscoveryFailed>();
    }

    [Fact]
    public async Task Given_A_Completed_Report_When_Handled_Then_No_Discovery_Lifecycle_Event_Is_Added()
    {
        var env = MusicCatalogLookupAttemptedHandlerTestEnvironment.Create();
        var before = env.StoredEvents("search:track:rare unknown song").Count;

        await env.Handler.Handle(
            new MusicCatalogLookupAttempted(
                CommandId.For("LookupMusicMetadata:mc_track_1"),
                MusicCatalogId.From("mc_track_1"),
                ProviderName.MusicBrainz,
                LookupPriorityBand.High,
                env.Now,
                CorrelationId.From("corr-1"),
                MusicCatalogLookupOutcome.Completed(),
                null),
            CancellationToken.None);

        env.StoredEvents("search:track:rare unknown song").Should().HaveCount(before);
    }
}
