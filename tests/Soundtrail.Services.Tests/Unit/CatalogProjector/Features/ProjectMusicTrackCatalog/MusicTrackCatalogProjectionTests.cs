using FluentAssertions;
using Soundtrail.Contracts.Common;
using Soundtrail.Domain.Catalog;
using Soundtrail.Domain.Events;
using Soundtrail.Services.Internal.Projector.Features.OnMusicCatalogChanged.ProjectionModel;

namespace Soundtrail.Services.Tests.Unit.CatalogProjector.Features.OnMusicCatalogChanged;

public sealed class MusicTrackCatalogProjectionTests
{
    [Fact]
    public void Given_A_Provider_Resolution_Before_Hierarchy_When_Artist_Arrives_Then_Artist_And_Album_Inherit_Provider_State()
    {
        var projection = MusicTrackCatalogProjection.Load(
            new MusicTrackCatalogProjectionSnapshot(
                MusicCatalogId.From("mc_track_1"),
                new CatalogTrackProjection(
                    "mc_track_1",
                    string.Empty,
                    "album_hot_fuss",
                    "Mr. Brightside",
                    "mr. brightside",
                    "The Killers",
                    string.Empty,
                    "mr. brightside the killers",
                    null,
                    null,
                    null,
                    [ProviderName.Spotify.Value],
                    [],
                    [],
                    "https://images.example.com/track.png",
                    Clock),
                null,
                new CatalogAlbumProjection(
                    "album_hot_fuss",
                    string.Empty,
                    string.Empty,
                    string.Empty,
                    string.Empty,
                    null,
                    [],
                    [],
                    null,
                    null,
                    Clock),
                1));

        projection.Apply(new ArtistDiscovered("artist_the_killers", "The Killers", "mb-artist-the-killers", ProviderName.MusicBrainz, Clock), 2);

        projection.Track.ArtistId.Should().Be("artist_the_killers");
        projection.Track.ArtistName.Should().Be("The Killers");
        projection.Artist.Should().NotBeNull();
        projection.Artist!.AvailableProviders.Should().Contain(ProviderName.Spotify.Value);
        projection.Artist.MusicBrainzArtistId.Should().Be("mb-artist-the-killers");
        projection.Artist.ArtworkUrl.Should().Be("https://images.example.com/track.png");
        projection.Album.Should().NotBeNull();
        projection.Album!.ArtistId.Should().Be("artist_the_killers");
        projection.Album.AvailableProviders.Should().Contain(ProviderName.Spotify.Value);
    }

    [Fact]
    public void Given_A_Provider_Failure_When_Applied_Then_Resolved_Providers_And_References_Are_Removed()
    {
        var projection = MusicTrackCatalogProjection.Load(
            new MusicTrackCatalogProjectionSnapshot(
                MusicCatalogId.From("mc_track_1"),
                new CatalogTrackProjection(
                    "mc_track_1",
                    "artist_the_killers",
                    "album_hot_fuss",
                    "Mr. Brightside",
                    "mr. brightside",
                    "The Killers",
                    "Hot Fuss",
                    "mr. brightside the killers",
                    null,
                    null,
                    null,
                    [ProviderName.YoutubeMusic.Value, ProviderName.Spotify.Value],
                    [],
                    [new CatalogProviderReferenceProjection(ProviderName.YoutubeMusic.Value, "track", "ytm-1", "https://music.youtube.com/watch?v=1", Clock)],
                    null,
                    Clock),
                new CatalogArtistProjection("artist_the_killers", string.Empty, string.Empty, null, [ProviderName.YoutubeMusic.Value], [], null, Clock),
                new CatalogAlbumProjection("album_hot_fuss", "artist_the_killers", string.Empty, string.Empty, string.Empty, null, [ProviderName.YoutubeMusic.Value], [], null, null, Clock),
                1));

        projection.Apply(new ProviderReferenceLookupFailed(ProviderName.YoutubeMusic, ProviderName.Odesli, Clock), 2);

        projection.Track.AvailableProviders.Should().NotContain(ProviderName.YoutubeMusic.Value);
        projection.Track.TerminallyUnavailableProviders.Should().Contain(ProviderName.YoutubeMusic.Value);
        projection.Track.ProviderReferences.Should().BeEmpty();
        projection.Artist!.AvailableProviders.Should().NotContain(ProviderName.YoutubeMusic.Value);
        projection.Artist.TerminallyUnavailableProviders.Should().Contain(ProviderName.YoutubeMusic.Value);
        projection.Album!.AvailableProviders.Should().NotContain(ProviderName.YoutubeMusic.Value);
        projection.Album.TerminallyUnavailableProviders.Should().Contain(ProviderName.YoutubeMusic.Value);
    }

    [Fact]
    public void Given_A_Metadata_Correction_When_Applied_Then_Track_Artist_And_Album_Are_Repaired()
    {
        var projection = new MusicTrackCatalogProjection(MusicCatalogId.From("mc_track_1"));
        projection.Apply(new TrackDiscovered("Mr Brightside", "Killers", 222000, "USIR20400274", "mbid-1", ProviderName.MusicBrainz, Clock), 1);

        projection.Apply(
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
                Clock),
            2);

        projection.Track.Title.Should().Be("Mr. Brightside");
        projection.Track.ArtistName.Should().Be("The Killers");
        projection.Track.ArtistId.Should().Be("artist_the_killers");
        projection.Track.AlbumId.Should().Be("album_hot_fuss");
        projection.Track.AlbumName.Should().Be("Hot Fuss");
        projection.Track.SearchText.Should().Be("mr. brightside the killers");
        projection.Artist!.Name.Should().Be("The Killers");
        projection.Artist.MusicBrainzArtistId.Should().Be("mb-artist-the-killers");
        projection.Album!.Name.Should().Be("Hot Fuss");
        projection.Album.ArtistId.Should().Be("artist_the_killers");
        projection.Album.MusicBrainzReleaseId.Should().Be("mb-release-hot-fuss");
        projection.Album.ReleaseDate.Should().Be(new DateOnly(2004, 6, 7));
    }

    [Fact]
    public void Given_Artist_Artwork_Event_When_Applied_Then_The_Resolved_Artist_Is_Updated()
    {
        var projection = new MusicTrackCatalogProjection(MusicCatalogId.From("mc_track_1"));

        projection.Apply(
            new ArtworkDiscovered(
                CatalogEntityKind.Artist,
                "artist_the_killers",
                new Uri("https://images.example.com/artist.png"),
                "worker/musicbrainz",
                Clock),
            1);

        projection.Artist.Should().NotBeNull();
        projection.Artist!.ArtistId.Should().Be("artist_the_killers");
        projection.Artist.ArtworkUrl.Should().Be("https://images.example.com/artist.png");
    }

    [Fact]
    public void Given_Provider_Resolution_Before_Hierarchy_When_Album_Arrives_Then_Album_Inherits_Track_State()
    {
        var projection = new MusicTrackCatalogProjection(MusicCatalogId.From("mc_track_1"));
        projection.Apply(new TrackDiscovered("Mr. Brightside", "The Killers", 222000, "USIR20400274", "mbid-1", ProviderName.MusicBrainz, Clock), 1);
        projection.Apply(new ProviderReferenceDiscovered(ProviderName.Spotify, "spotify-1", new Uri("https://example.com/spotify-1"), ProviderName.Odesli, Clock), 2);
        projection.Apply(new ArtworkDiscovered(CatalogEntityKind.Track, null, new Uri("https://images.example.com/track.png"), "worker/musicbrainz", Clock), 3);

        projection.Apply(new AlbumDiscovered("album_hot_fuss", "Hot Fuss", "mb-release-hot-fuss", new DateOnly(2004, 6, 7), ProviderName.MusicBrainz, Clock), 4);

        projection.Album.Should().NotBeNull();
        projection.Album!.AvailableProviders.Should().Contain(ProviderName.Spotify.Value);
        projection.Album.MusicBrainzReleaseId.Should().Be("mb-release-hot-fuss");
        projection.Album.ArtworkUrl.Should().Be("https://images.example.com/track.png");
        projection.Album.ReleaseDate.Should().Be(new DateOnly(2004, 6, 7));
    }

    private static readonly DateTimeOffset Clock = new(2026, 6, 18, 12, 0, 0, TimeSpan.Zero);
}
