using FluentAssertions;
using Soundtrail.Contracts.Common;
using Soundtrail.Domain.Catalog;
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

    [Fact]
    public async Task Given_Imported_Metadata_Correction_And_Artwork_Events_When_Projection_Replays_Then_Catalog_Documents_Are_Rebuilt_From_The_Stored_Event_Vocabulary()
    {
        await using var env = RavenMusicTrackEventImportTestEnvironment.Create();
        var observedAt = new DateTimeOffset(2026, 6, 16, 12, 0, 0, TimeSpan.Zero);
        var command = new ImportMusicTrackEventsCommand(
            MusicCatalogId.From("mc_track_1"),
            0,
            CommandId.For("ImportMusicTrackEvents:mc_track_1"),
            [
                new TrackDiscovered("Mr Brightside", "Killers", 222000, "USIR20400274", "mbid-1", ProviderName.MusicBrainz, observedAt),
                new MetadataCorrected(
                    "Mr. Brightside",
                    "The Killers",
                    "artist_the_killers",
                    "mb-artist-the-killers",
                    "Hot Fuss",
                    "album_hot_fuss",
                    "mb-release-hot-fuss",
                    new DateOnly(2004, 6, 7),
                    222000,
                    "USIR20400274",
                    "mbid-1",
                    "admin/repair",
                    observedAt.AddMinutes(1)),
                new ArtworkDiscovered(
                    CatalogEntityKind.Track,
                    null,
                    new Uri("https://images.example.com/track.png"),
                    "worker/musicbrainz",
                    observedAt.AddMinutes(2)),
                new ArtworkDiscovered(
                    CatalogEntityKind.Artist,
                    "artist_the_killers",
                    new Uri("https://images.example.com/artist.png"),
                    "worker/musicbrainz",
                    observedAt.AddMinutes(3)),
                new ArtworkDiscovered(
                    CatalogEntityKind.Album,
                    "album_hot_fuss",
                    new Uri("https://images.example.com/album.png"),
                    "worker/musicbrainz",
                    observedAt.AddMinutes(4))
            ]);

        await env.ImportAsync(command);
        await env.ReplayCatalogProjectionAsync();

        var track = await env.LoadCatalogTrackAsync("mc_track_1");
        var artist = await env.LoadCatalogArtistAsync("artist_the_killers");
        var album = await env.LoadCatalogAlbumAsync("album_hot_fuss");

        track.Should().NotBeNull();
        track!.Title.Should().Be("Mr. Brightside");
        track.ArtistId.Should().Be("artist_the_killers");
        track.ArtistName.Should().Be("The Killers");
        track.AlbumId.Should().Be("album_hot_fuss");
        track.AlbumName.Should().Be("Hot Fuss");
        track.ArtworkUrl.Should().Be("https://images.example.com/track.png");

        artist.Should().NotBeNull();
        artist!.Name.Should().Be("The Killers");
        artist.MusicBrainzArtistId.Should().Be("mb-artist-the-killers");
        artist.ArtworkUrl.Should().Be("https://images.example.com/artist.png");

        album.Should().NotBeNull();
        album!.Name.Should().Be("Hot Fuss");
        album.ArtistId.Should().Be("artist_the_killers");
        album.ArtistName.Should().Be("The Killers");
        album.MusicBrainzReleaseId.Should().Be("mb-release-hot-fuss");
        album.ReleaseDate.Should().Be(new DateOnly(2004, 6, 7));
        album.ArtworkUrl.Should().Be("https://images.example.com/album.png");
    }
}
