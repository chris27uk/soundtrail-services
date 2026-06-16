using FluentAssertions;
using Soundtrail.Contracts.Common;
using Soundtrail.Domain.Commands;
using Soundtrail.Domain.Events;
using Soundtrail.Services.Enrichment.DiscoveryPlanner.Features.ImportMusicTrackEvents;
using Soundtrail.Services.Tests.Unit.Enrichment.Infrastructure;

namespace Soundtrail.Services.Tests.Unit.Enrichment.Features.ImportMusicTrackEvents;

public sealed class ImportMusicTrackEventsHandlerTests
{
    [Fact]
    public async Task Given_Music_Track_Events_When_Imported_Then_They_Are_Appended_To_The_Event_Store()
    {
        var repository = new MusicTrackStreamStoreFake();
        var handler = new ImportMusicTrackEventsHandler(repository);
        var command = new ImportMusicTrackEventsCommand(
            MusicCatalogId.From("mc_track_1"),
            0,
            CommandId.For("ImportMusicTrackEvents:mc_track_1"),
            [new TrackDiscovered("Song A", "Artist A", 123000, "isrc-1", "mbid-1", ProviderName.MusicBrainz, new DateTimeOffset(2026, 6, 16, 12, 0, 0, TimeSpan.Zero))]);

        var result = await handler.Handle(command, CancellationToken.None);

        result.Appended.Should().BeTrue();
        result.ImportedEventCount.Should().Be(1);
        repository.Streams["mc_track_1"].Events.Should().ContainSingle().Which.Should().BeOfType<TrackDiscovered>();
    }
}
