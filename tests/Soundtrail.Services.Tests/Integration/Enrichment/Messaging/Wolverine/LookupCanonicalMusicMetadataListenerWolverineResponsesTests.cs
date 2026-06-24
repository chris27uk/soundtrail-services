using FluentAssertions;
using Soundtrail.Contracts.Common;
using Soundtrail.Contracts.IntegrationMessaging.Commands;
using Soundtrail.Contracts.IntegrationMessaging.Responses;
using Soundtrail.Domain.Model;
using Soundtrail.Domain.Responses;
using Soundtrail.Services.Enrichment.Worker.Features.OnLookupCanonicalMusicMetadata.Adapters;
using Soundtrail.Services.Tests.Unit.Enrichment.Infrastructure;

namespace Soundtrail.Services.Tests.Integration.Enrichment.Messaging.Wolverine;

public sealed class LookupCanonicalMusicMetadataListenerWolverineResponsesTests
{
    [Fact]
    public async Task Given_A_Successful_Metadata_Lookup_When_Handled_Then_An_Enrichment_Response_Message_Is_Returned()
    {
        var env = LookupCanonicalMusicMetadataHandlerTestEnvironment.Create();
        env.SeedMusicBrainzIsrc("isrc-1", new SongMetadata("Song A", "Artist A", "isrc-1", "mbid-1", 123000, "Album A", new DateOnly(2004, 6, 7), "mb-artist-1", "mb-release-1"));
        var bus = new WolverineMessageBusFake();
        var listener = new LookupCanonicalMusicMetadataListener(env.Handler, bus);

        await listener.Handle(Command(), null!);

        bus.SentMessages.Should().ContainSingle().Which.Should().BeOfType<MusicCatalogLookupAttemptedDto>();
    }

    [Fact]
    public async Task Given_A_Budget_Deferred_Metadata_Lookup_When_Handled_Then_A_Lookup_Execution_Report_Is_Returned()
    {
        var env = LookupCanonicalMusicMetadataHandlerTestEnvironment.Create();
        env.SourceBudget.Reject(
            ProviderName.MusicBrainz,
            new DateTimeOffset(2026, 6, 18, 12, 1, 0, TimeSpan.Zero),
            "MusicBrainz budget temporarily unavailable");
        var bus = new WolverineMessageBusFake();
        var listener = new LookupCanonicalMusicMetadataListener(env.Handler, bus);

        await listener.Handle(Command(), null!);
        var message = bus.SentMessages.Single().Should().BeOfType<MusicCatalogLookupAttemptedDto>().Subject;

        message.Outcome.Status.Should().Be("Deferred");
        message.SourceProvider.Should().Be(ProviderName.MusicBrainz.Value);
        message.Outcome.Reason.Should().Be("MusicBrainz budget temporarily unavailable");
    }

    [Fact]
    public async Task Given_A_Failed_Metadata_Lookup_When_Handled_Then_A_Lookup_Execution_Report_Is_Returned()
    {
        var env = LookupCanonicalMusicMetadataHandlerTestEnvironment.Create();
        env.Throw(new InvalidOperationException("boom"));
        var bus = new WolverineMessageBusFake();
        var listener = new LookupCanonicalMusicMetadataListener(env.Handler, bus);

        await listener.Handle(Command(), null!);
        var message = bus.SentMessages.Single().Should().BeOfType<MusicCatalogLookupAttemptedDto>().Subject;

        message.Outcome.Status.Should().Be("Failed");
        message.SourceProvider.Should().Be(ProviderName.MusicBrainz.Value);
        message.Outcome.Reason.Should().Be("Lookup failed");
    }

    private static LookupCanonicalMusicMetadataCommandDto Command() =>
        new(
            CommandId.For("LookupCanonicalMusicMetadata:mc_track_1").Value,
            "mc_track_1",
            LookupPriorityBand.High,
            new DateTimeOffset(2026, 6, 18, 12, 0, 0, TimeSpan.Zero),
            "corr-1",
            "isrc-1",
            null,
            null,
            null,
            null,
            null);
}
