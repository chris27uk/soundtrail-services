using FluentAssertions;
using Soundtrail.Contracts.Common;
using Soundtrail.Contracts.IntegrationMessaging.Commands;
using Soundtrail.Contracts.IntegrationMessaging.Responses;
using Soundtrail.Adapters.Registry;
using Soundtrail.Domain.Enrichment.Commands;
using Soundtrail.Domain.Enrichment.Responses;
using Soundtrail.Domain.Search;
using Soundtrail.Adapters.Messaging;
using Soundtrail.Services.Enrichment.Worker.Features.OnLookupStreamingLocations.Adapters;
using Soundtrail.Services.Tests.Unit.Enrichment.Infrastructure;

namespace Soundtrail.Services.Tests.Integration.Enrichment.Messaging.Wolverine;

public sealed class LookupStreamingLocationsListenerWolverineResponsesTests
{
    [Fact]
    public async Task Given_A_Successful_Playback_Reference_Lookup_When_Handled_Then_An_Enrichment_Response_Message_Is_Returned()
    {
        var env = LookupStreamingLocationsHandlerTestEnvironment.Create();
        env.Seed(
            MusicSearchCriteria.ByIsrc("isrc-1"),
            new ExternalReference(ProviderName.AppleMusic, new Uri("https://music.apple.com/us/song/apple-1?i=apple-1"), "apple-1"));
        var listener = new LookupStreamingLocationsListener(env.Handler);

        await listener.Handle(Command(), null!);

        env.Bus.SentCommands.Should().ContainSingle().Which.Should().BeOfType<CatalogItemLookupAttempted>();
        TypeTranslationRegistry.Default
            .ToDto<CatalogItemLookupAttemptedDto>(env.Bus.SentCommands
                .OfType<CatalogItemLookupAttempted>()
                .Single())
            .Should()
            .BeOfType<CatalogItemLookupAttemptedDto>();
    }

    [Fact]
    public async Task Given_A_Budget_Deferred_Playback_Reference_Lookup_When_Handled_Then_A_Lookup_Execution_Report_Is_Returned()
    {
        var env = LookupStreamingLocationsHandlerTestEnvironment.Create();
        env.Admission.Reject(
            LookupSource.Odesli,
            new DateTimeOffset(2026, 6, 18, 12, 1, 0, TimeSpan.Zero),
            "Odesli budget temporarily unavailable");
        var listener = new LookupStreamingLocationsListener(env.Handler);

        await listener.Handle(Command(), null!);
        var message = TypeTranslationRegistry.Default.ToDto<CatalogItemLookupAttemptedDto>(
            env.Bus.SentCommands.OfType<CatalogItemLookupAttempted>().Single());

        message.Outcome.Status.Should().Be("Deferred");
        message.SourceProvider.Should().Be(LookupSource.Odesli.Value);
        message.Outcome.Reason.Should().Be("Odesli budget temporarily unavailable");
    }

    [Fact]
    public async Task Given_A_Failed_Playback_Reference_Lookup_When_Handled_Then_A_Lookup_Execution_Report_Is_Returned()
    {
        var env = LookupStreamingLocationsHandlerTestEnvironment.Create();
        env.Throw(new InvalidOperationException("boom"));
        var listener = new LookupStreamingLocationsListener(env.Handler);

        await listener.Handle(Command(), null!);
        var message = TypeTranslationRegistry.Default.ToDto<CatalogItemLookupAttemptedDto>(
            env.Bus.SentCommands.OfType<CatalogItemLookupAttempted>().Single());

        message.Outcome.Status.Should().Be("Failed");
        message.SourceProvider.Should().Be(LookupSource.Odesli.Value);
        message.Outcome.Reason.Should().Be("Lookup failed");
    }

    [Fact]
    public async Task Given_A_TrackArtistAlbum_StreamingLocations_Command_When_Isrc_Is_Also_Present_Then_The_Declared_Search_Kind_Is_Used()
    {
        var env = LookupStreamingLocationsHandlerTestEnvironment.Create();
        env.Seed(
            MusicSearchCriteria.ByTrackArtistAlbum("Song A", "Artist A", "Album A"),
            new ExternalReference(ProviderName.AppleMusic, new Uri("https://music.apple.com/us/song/apple-1?i=apple-1"), "apple-1"));
        var listener = new LookupStreamingLocationsListener(env.Handler);

        await listener.Handle(
            new LookupStreamingLocationsCommandDto(
                CommandId.For("LookupStreamingLocations:mc_track_1").Value,
                "mc_track_1",
                LookupPriorityBand.High,
                new DateTimeOffset(2026, 6, 18, 12, 0, 0, TimeSpan.Zero),
                "corr-1",
                new StreamingLocationSearchTermDto(
                    MusicSearchKind.TrackArtistAlbum,
                    null,
                    "isrc-should-not-win",
                    "Song A",
                    "Artist A",
                    "Album A"),
                null,
                null),
            null!);

        env.References.SearchTerms.Should().ContainSingle()
            .Which.Should().Be(MusicSearchCriteria.ByTrackArtistAlbum("Song A", "Artist A", "Album A"));
    }

    private static LookupStreamingLocationsCommandDto Command() =>
        new(
            CommandId.For("LookupStreamingLocations:mc_track_1").Value,
            "mc_track_1",
            LookupPriorityBand.High,
            new DateTimeOffset(2026, 6, 18, 12, 0, 0, TimeSpan.Zero),
            "corr-1",
            new StreamingLocationSearchTermDto(MusicSearchKind.Isrc, null, "isrc-1", null, null, null),
            null,
            null);
}
