using FluentAssertions;
using Soundtrail.Contracts.Common;
using Soundtrail.Contracts.IntegrationMessaging.Responses;
using Soundtrail.Domain.Enrichment.Commands;
using Soundtrail.Domain.Enrichment.Responses;
using Soundtrail.Services.Enrichment.Orchestrator.Features.OnMusicCatalogLookupAttempted.Adapters;
using Soundtrail.Services.Tests.Unit.Enrichment.Infrastructure;

namespace Soundtrail.Services.Tests.Integration.Enrichment.Messaging.Wolverine;

public sealed class MusicCatalogLookupAttemptedListenerWolverineResponsesTests
{
    [Fact]
    public async Task Given_A_Deferred_Report_When_Handled_Then_Catalog_And_Discovery_Follow_Up_Commands_Are_Sent()
    {
        var env = MusicCatalogLookupAttemptedHandlerTestEnvironment.Create();
        var listener = new MusicCatalogLookupAttemptedListener(env.Handler);

        await listener.Handle(
            new MusicCatalogLookupAttemptedDto(
                "LookupTrackMetadata:mc_track_1",
                "mc_track_1",
                LookupSource.MusicBrainz.Value,
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

        env.Bus.SentCommands.Should().ContainSingle(x => x is ApplyMusicCatalogLookupAttemptedToCatalogCommand);
        env.Bus.SentCommands.Should().ContainSingle(x => x is ApplyMusicCatalogLookupAttemptedToDiscoveryCommand);
    }

    [Fact]
    public async Task Given_A_Failed_Report_When_Handled_Then_The_Original_Lookup_Attempt_Is_Preserved_In_Both_Follow_Up_Commands()
    {
        var env = MusicCatalogLookupAttemptedHandlerTestEnvironment.Create();
        var listener = new MusicCatalogLookupAttemptedListener(env.Handler);

        await listener.Handle(
            new MusicCatalogLookupAttemptedDto(
                "LookupTrackMetadata:mc_track_1",
                "mc_track_1",
                LookupSource.MusicBrainz.Value,
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

        env.Bus.SentCommands.OfType<ApplyMusicCatalogLookupAttemptedToCatalogCommand>().Single().Attempted.Outcome.Status
            .Should().Be(MusicCatalogLookupOutcomeStatus.Failed);
        env.Bus.SentCommands.OfType<ApplyMusicCatalogLookupAttemptedToDiscoveryCommand>().Single().Attempted.Outcome.Status
            .Should().Be(MusicCatalogLookupOutcomeStatus.Failed);
    }
}
