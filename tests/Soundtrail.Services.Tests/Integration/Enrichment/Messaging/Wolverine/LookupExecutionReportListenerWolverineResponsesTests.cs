using FluentAssertions;
using Soundtrail.Contracts.Common;
using Soundtrail.Contracts.IntegrationMessaging.Responses;
using Soundtrail.Domain.Discovery;
using Soundtrail.Domain.Events;
using Soundtrail.Services.Enrichment.DiscoveryPlanner.Features.ApplyLookupExecutionReport.Adapters;
using Soundtrail.Services.Tests.Unit.Enrichment.Infrastructure;

namespace Soundtrail.Services.Tests.Integration.Enrichment.Messaging.Wolverine;

public sealed class LookupExecutionReportListenerWolverineResponsesTests
{
    [Fact]
    public async Task Given_A_Deferred_Report_When_Handled_Then_Discovery_Is_Deferred()
    {
        var env = ApplyLookupExecutionReportHandlerTestEnvironment.Create();
        var listener = new LookupExecutionReportListener(env.Handler);

        await listener.Handle(
            new LookupExecutionReportDto(
                "LookupCanonicalMusicMetadata:mc_track_1",
                "mc_track_1",
                ProviderName.MusicBrainz.Value,
                LookupPriorityBand.High,
                env.Now,
                "corr-1",
                "Deferred",
                "MusicBrainz budget temporarily unavailable",
                env.Now.AddMinutes(1),
                60),
            null!);

        env.StoredEvents("search:track:rare unknown song").Last().Should().BeOfType<DiscoveryDeferred>();
    }

    [Fact]
    public async Task Given_A_Failed_Report_When_Handled_Then_Discovery_Is_Failed()
    {
        var env = ApplyLookupExecutionReportHandlerTestEnvironment.Create();
        var listener = new LookupExecutionReportListener(env.Handler);

        await listener.Handle(
            new LookupExecutionReportDto(
                "LookupCanonicalMusicMetadata:mc_track_1",
                "mc_track_1",
                ProviderName.MusicBrainz.Value,
                LookupPriorityBand.High,
                env.Now,
                "corr-1",
                "Failed",
                "Lookup failed",
                null,
                null),
            null!);

        env.StoredEvents("search:track:rare unknown song").Last().Should().BeOfType<DiscoveryFailed>();
    }
}
