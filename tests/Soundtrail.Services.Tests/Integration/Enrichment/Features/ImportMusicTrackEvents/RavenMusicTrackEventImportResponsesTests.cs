using FluentAssertions;
using Soundtrail.Contracts.Common;
using Soundtrail.Domain.Commands;
using Soundtrail.Domain.Events;
using Soundtrail.Services.Tests.Integration.Api.Infrastructure;

namespace Soundtrail.Services.Tests.Integration.Enrichment.Features.ImportMusicTrackEvents;

[Collection(RavenEmbeddedCollection.Name)]
public sealed class RavenMusicTrackEventImportResponsesTests
{
    [Fact]
    public async Task Given_Imported_Music_Track_Events_When_Projection_Has_Not_Replayed_Yet_Then_No_Catalog_Documents_Are_Written_Directly()
    {
        await using var env = RavenMusicTrackEventImportTestEnvironment.Create();
        var command = new ImportMusicTrackEventsCommand(
            MusicCatalogId.From("mc_track_1"),
            0,
            CommandId.For("ImportMusicTrackEvents:mc_track_1"),
            [new TrackDiscovered("Song A", "Artist A", 123000, "isrc-1", "mbid-1", ProviderName.MusicBrainz, new DateTimeOffset(2026, 6, 16, 12, 0, 0, TimeSpan.Zero))]);

        await env.ImportAsync(command);

        var track = await env.LoadCatalogTrackAsync("mc_track_1");

        track.Should().BeNull();
    }

    [Fact]
    public async Task Given_Imported_Music_Track_Events_When_Projection_Replays_Then_Catalog_Documents_Are_Built_From_Stored_Events()
    {
        await using var env = RavenMusicTrackEventImportTestEnvironment.Create();
        var command = new ImportMusicTrackEventsCommand(
            MusicCatalogId.From("mc_track_1"),
            0,
            CommandId.For("ImportMusicTrackEvents:mc_track_1"),
            [
                new TrackDiscovered("Mr. Brightside", "The Killers", 222000, "USIR20400274", "mbid-1", ProviderName.MusicBrainz, new DateTimeOffset(2026, 6, 16, 12, 0, 0, TimeSpan.Zero)),
                new ArtistDiscovered("artist_the_killers", "The Killers", "mb-artist-the-killers", ProviderName.MusicBrainz, new DateTimeOffset(2026, 6, 16, 12, 1, 0, TimeSpan.Zero)),
                new AlbumDiscovered("album_hot_fuss", "Hot Fuss", "mb-release-hot-fuss", new DateOnly(2004, 6, 7), ProviderName.MusicBrainz, new DateTimeOffset(2026, 6, 16, 12, 2, 0, TimeSpan.Zero))
            ]);

        await env.ImportAsync(command);
        await env.ReplayCatalogProjectionAsync();

        var track = await env.LoadCatalogTrackAsync("mc_track_1");

        track.Should().NotBeNull();
        track!.Title.Should().Be("Mr. Brightside");
        track.ArtistId.Should().Be("artist_the_killers");
        track.AlbumId.Should().Be("album_hot_fuss");
        track.AlbumName.Should().Be("Hot Fuss");
    }
}
