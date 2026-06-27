using FluentAssertions;
using Soundtrail.Contracts.Common;
using Soundtrail.Contracts.IntegrationMessaging.Responses;
using Soundtrail.Domain.Discovery;
using Soundtrail.Domain.Events;
using Soundtrail.Services.Enrichment.Orchestrator.Features.OnMusicCatalogLookupAttempted.Adapters;
using Soundtrail.Services.Tests.Unit.Enrichment.Infrastructure;

namespace Soundtrail.Services.Tests.Integration.Enrichment.Messaging.Wolverine;

public sealed class MusicCatalogLookupAttemptedListenerWolverineResponsesTests
{
    [Fact]
    public async Task Given_A_Deferred_Report_When_Handled_Then_Discovery_Is_Deferred()
    {
        var env = MusicCatalogLookupAttemptedHandlerTestEnvironment.Create();
        var listener = new MusicCatalogLookupAttemptedListener(env.Handler);

        await listener.Handle(
            new MusicCatalogLookupAttemptedDto(
                "LookupTrackMetadata:mc_track_1",
                "mc_track_1",
                ProviderName.MusicBrainz.Value,
                LookupPriorityBand.High,
                env.Now,
                "corr-1",
                new MusicCatalogLookupOutcomeDto(
                    "Deferred",
                    "MusicBrainz budget temporarily unavailable",
                    env.Now.AddMinutes(1),
                    60),
                null),
            null!);

        env.StoredEvents("search:track:rare unknown song").Last().Should().BeOfType<DiscoveryDeferred>();
    }

    [Fact]
    public async Task Given_A_Failed_Report_When_Handled_Then_Discovery_Is_Failed()
    {
        var env = MusicCatalogLookupAttemptedHandlerTestEnvironment.Create();
        var listener = new MusicCatalogLookupAttemptedListener(env.Handler);

        await listener.Handle(
            new MusicCatalogLookupAttemptedDto(
                "LookupTrackMetadata:mc_track_1",
                "mc_track_1",
                ProviderName.MusicBrainz.Value,
                LookupPriorityBand.High,
                env.Now,
                "corr-1",
                new MusicCatalogLookupOutcomeDto(
                    "Failed",
                    "Lookup failed",
                    null,
                    null),
                null),
            null!);

        env.StoredEvents("search:track:rare unknown song").Last().Should().BeOfType<DiscoveryFailed>();
    }
}
