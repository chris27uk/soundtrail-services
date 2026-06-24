using FluentAssertions;
using Soundtrail.Domain.Catalog.IntegrationEvents;
using Soundtrail.Services.Tests.Unit.Enrichment.Infrastructure;

namespace Soundtrail.Services.Tests.Unit.Enrichment.Features.PublishMusicTrackEvents;

public sealed class PublishMusicTrackEventsHandlerTests
{
    [Fact]
    public async Task Given_A_Playback_References_Resolution_Required_Event_When_Handled_Then_The_Integration_Message_Is_Published()
    {
        var env = PublishMusicTrackEventsHandlerTestEnvironment.Create();

        await env.HandleAsync(
            PublishMusicTrackEventsHandlerTestEnvironment.StreamingLocationsRequired("mc_track_1", 1));

        env.Publisher.PublishedBatches.Should().ContainSingle();
        env.Publisher.PublishedBatches[0].Should().ContainSingle().Which.Should().BeOfType<PlaybackReferencesResolutionRequiredIntegrationEvent>();
    }

    [Fact]
    public async Task Given_An_Unsupported_Event_When_Handled_Then_Nothing_Is_Published()
    {
        var env = PublishMusicTrackEventsHandlerTestEnvironment.Create();

        await env.Handler.Handle(
            PublishMusicTrackEventsHandlerTestEnvironment.EmptyCommand(),
            CancellationToken.None);

        env.Publisher.PublishedBatches.Should().BeEmpty();
    }

    [Fact]
    public async Task Given_Multiple_Events_When_Handled_Then_They_Are_Published_In_Stream_Order()
    {
        var env = PublishMusicTrackEventsHandlerTestEnvironment.Create();

        await env.HandleAsync(
            PublishMusicTrackEventsHandlerTestEnvironment.StreamingLocationsRequired("mc_track_2", 3, title: "Song B", artist: "Artist B"),
            PublishMusicTrackEventsHandlerTestEnvironment.StreamingLocationsRequired("mc_track_1", 2));

        env.Publisher.PublishedBatches.Should().ContainSingle();
        var batch = env.Publisher.PublishedBatches[0].OfType<PlaybackReferencesResolutionRequiredIntegrationEvent>().ToArray();
        batch.Should().HaveCount(2);
        batch[0].MusicCatalogId.Value.Should().Be("mc_track_1");
        batch[1].MusicCatalogId.Value.Should().Be("mc_track_2");
    }
}
