using FluentAssertions;
using Soundtrail.Contracts.Common;
using Soundtrail.Contracts.IntegrationMessaging.Responses;
using Soundtrail.Domain.Enrichment;
using Soundtrail.Domain.Enrichment.Events;
using Soundtrail.Domain.Enrichment.Responses;
using Soundtrail.Services.Enrichment.Orchestrator.Features.OnCatalogItemLookupAttempted;
using Soundtrail.Services.Enrichment.Orchestrator.Features.OnCatalogItemLookupAttempted.Adapters;
using Soundtrail.Services.Enrichment.Orchestrator.Features.OnMusicCatalogLookupAttempted.Adapters;
using Soundtrail.Services.Tests.Unit.Enrichment.Infrastructure;

namespace Soundtrail.Services.Tests.Integration.Enrichment.Messaging.Wolverine;

public sealed class MusicCatalogLookupAttemptedListenerWolverineResponsesTests
{
    [Fact]
    public async Task Given_A_Deferred_Report_When_Handled_Then_Lookup_History_Is_Written()
    {
        var env = MusicCatalogLookupAttemptedHandlerTestEnvironment.Create();
        var listener = new CatalogItemLookupAttemptedListener(
            new CatalogItemLookupAttemptedHandler(env.Handler, null!, null!));

        await listener.Handle(
            new CatalogItemLookupAttemptedDto(
                "LookupTrackMetadata:mc_track_1",
                CatalogItemKind.Track,
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
                null,
                null,
                null),
            null!);

        env.LookupHistoryRepository
            .GetStoredEvents(MusicCatalogLookupId.From(MusicCatalogId.From("mc_track_1")))
            .Should().ContainSingle()
            .Which.Should().BeOfType<MusicCatalogLookupDeferred>();
    }

    [Fact]
    public async Task Given_A_Failed_Report_When_Handled_Then_The_Lookup_History_Preserves_The_Failure_Outcome()
    {
        var env = MusicCatalogLookupAttemptedHandlerTestEnvironment.Create();
        var listener = new CatalogItemLookupAttemptedListener(
            new CatalogItemLookupAttemptedHandler(env.Handler, null!, null!));

        await listener.Handle(
            new CatalogItemLookupAttemptedDto(
                "LookupTrackMetadata:mc_track_1",
                CatalogItemKind.Track,
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
                null,
                null,
                null),
            null!);

        env.LookupHistoryRepository
            .GetStoredEvents(MusicCatalogLookupId.From(MusicCatalogId.From("mc_track_1")))
            .Single().Should().BeOfType<MusicCatalogLookupFailed>()
            .Which.Reason.Should().Be("Lookup failed");
    }
}
