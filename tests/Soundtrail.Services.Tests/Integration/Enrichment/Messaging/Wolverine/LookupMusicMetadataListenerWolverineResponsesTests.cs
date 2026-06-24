using FluentAssertions;
using Soundtrail.Contracts.Common;
using Soundtrail.Contracts.IntegrationMessaging.Commands;
using Soundtrail.Contracts.IntegrationMessaging.Responses;
using Soundtrail.Domain.Model;
using Soundtrail.Domain.Responses;
using Soundtrail.Services.Enrichment.Worker.Features.OnLookupMusicMetadata.Adapters;
using Soundtrail.Services.Tests.Unit.Enrichment.Infrastructure;

namespace Soundtrail.Services.Tests.Integration.Enrichment.Messaging.Wolverine;

public sealed class LookupMusicMetadataListenerWolverineResponsesTests
{
    [Fact]
    public async Task Given_A_Successful_Metadata_Lookup_When_Handled_Then_An_Enrichment_Response_Message_Is_Returned()
    {
        var env = LookupMusicMetadataHandlerTestEnvironment.Create();
        env.SeedMusicBrainzIsrc("isrc-1", new SongMetadata("Song A", "Artist A", "isrc-1", "mbid-1", 123000, "Album A", new DateOnly(2004, 6, 7), "mb-artist-1", "mb-release-1"));
        var bus = new WolverineMessageBusFake();
        var listener = new LookupMusicMetadataListener(env.Handler, bus);

        await listener.Handle(Command(), null!);

        bus.SentMessages.Should().ContainSingle().Which.Should().BeOfType<MusicCatalogLookupAttemptedDto>();
    }

    [Fact]
    public async Task Given_A_Budget_Deferred_Metadata_Lookup_When_Handled_Then_A_Lookup_Execution_Report_Is_Returned()
    {
        var env = LookupMusicMetadataHandlerTestEnvironment.Create();
        env.SourceBudget.Reject(
            ProviderName.MusicBrainz,
            new DateTimeOffset(2026, 6, 18, 12, 1, 0, TimeSpan.Zero),
            "MusicBrainz budget temporarily unavailable");
        var bus = new WolverineMessageBusFake();
        var listener = new LookupMusicMetadataListener(env.Handler, bus);

        await listener.Handle(Command(), null!);
        var message = bus.SentMessages.Single().Should().BeOfType<MusicCatalogLookupAttemptedDto>().Subject;

        message.Outcome.Status.Should().Be("Deferred");
        message.SourceProvider.Should().Be(ProviderName.MusicBrainz.Value);
        message.Outcome.Reason.Should().Be("MusicBrainz budget temporarily unavailable");
    }

    [Fact]
    public async Task Given_A_Failed_Metadata_Lookup_When_Handled_Then_A_Lookup_Execution_Report_Is_Returned()
    {
        var env = LookupMusicMetadataHandlerTestEnvironment.Create();
        env.Throw(new InvalidOperationException("boom"));
        var bus = new WolverineMessageBusFake();
        var listener = new LookupMusicMetadataListener(env.Handler, bus);

        await listener.Handle(Command(), null!);
        var message = bus.SentMessages.Single().Should().BeOfType<MusicCatalogLookupAttemptedDto>().Subject;

        message.Outcome.Status.Should().Be("Failed");
        message.SourceProvider.Should().Be(ProviderName.MusicBrainz.Value);
        message.Outcome.Reason.Should().Be("Lookup failed");
    }

    [Fact]
    public async Task Given_A_TrackArtistAlbum_Metadata_Command_When_Isrc_Is_Also_Present_Then_The_Declared_Search_Kind_Is_Used()
    {
        var env = LookupMusicMetadataHandlerTestEnvironment.Create();
        env.SeedMusicBrainzNames("Song A", "Artist A", "Album A", new SongMetadata("Song A", "Artist A", "isrc-1", "mbid-1", 123000, "Album A", new DateOnly(2004, 6, 7), "mb-artist-1", "mb-release-1"));
        var bus = new WolverineMessageBusFake();
        var listener = new LookupMusicMetadataListener(env.Handler, bus);

        await listener.Handle(
            new LookupMusicMetadataCommandDto(
                CommandId.For("LookupMusicMetadata:mc_track_1").Value,
                "mc_track_1",
                LookupPriorityBand.High,
                new DateTimeOffset(2026, 6, 18, 12, 0, 0, TimeSpan.Zero),
                "corr-1",
                MusicSearchKind.TrackArtistAlbum,
                null,
                "isrc-should-not-win",
                "Song A",
                "Artist A",
                "Album A",
                null,
                null),
            null!);

        env.Metadata.Lookups.Should().ContainSingle().Which.Should().StartWith("names:");
    }

    [Fact]
    public async Task Given_A_Unified_Metadata_Command_When_Handled_Then_The_Query_Is_Preserved()
    {
        var env = LookupMusicMetadataHandlerTestEnvironment.Create();
        env.SeedMusicBrainzQuery("rare unknown song", new SongMetadata("Rare Unknown Song", "Test Artist", "isrc-rare-1", "mbid-1", 123000, "Rare Album", null, "mb-artist-1", "mb-release-1"));
        var bus = new WolverineMessageBusFake();
        var listener = new LookupMusicMetadataListener(env.Handler, bus);

        await listener.Handle(
            new LookupMusicMetadataCommandDto(
                CommandId.For("LookupMusicMetadata:mc_track_1").Value,
                "mc_track_1",
                LookupPriorityBand.High,
                new DateTimeOffset(2026, 6, 18, 12, 0, 0, TimeSpan.Zero),
                "corr-1",
                MusicSearchKind.UnifiedSearch,
                "rare unknown song",
                "isrc-should-not-win",
                "Wrong Song",
                "Wrong Artist",
                "Wrong Album",
                null,
                null),
            null!);

        env.Metadata.Lookups.Should().ContainSingle().Which.Should().Be("query:rare unknown song");
    }

    private static LookupMusicMetadataCommandDto Command() =>
        new(
            CommandId.For("LookupMusicMetadata:mc_track_1").Value,
            "mc_track_1",
            LookupPriorityBand.High,
            new DateTimeOffset(2026, 6, 18, 12, 0, 0, TimeSpan.Zero),
            "corr-1",
            MusicSearchKind.Isrc,
            null,
            "isrc-1",
            null,
            null,
            null,
            null,
            null);
}
