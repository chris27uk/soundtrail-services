using FluentAssertions;
using Soundtrail.Contracts.Common;
using Soundtrail.Domain.Catalog;
using Soundtrail.Domain.Events;
using Soundtrail.Domain.Model;

namespace Soundtrail.Services.Tests.Unit.Enrichment.Features.Projection;

public sealed class MusicTrackProjectionTests
{
    [Fact]
    public void Given_Resolved_Metadata_And_Apple_Reference_When_Applied_Then_The_Track_Becomes_Playable()
    {
        var projection = new MusicTrackProjection();

        projection.Apply(
            new TrackDiscovered("Song A", "Artist A", 123000, "isrc-1", "mbid-1", ProviderName.MusicBrainz, Clock),
            1);
        projection.Apply(
            new ProviderReferenceDiscovered(
                ProviderName.AppleMusic,
                "apple-1",
                new Uri("https://music.apple.com/us/song/song-a?i=apple-1"),
                ProviderName.Odesli,
                Clock.AddMinutes(1)),
            2);

        projection.Title.Should().Be("Song A");
        projection.Artist.Value.Should().Be("Artist A");
        projection.Artist.Normalized.Should().Be("artist a");
        projection.AppleId.Should().Be("apple-1");
        projection.IsPlayable.Should().BeTrue();
        projection.ProjectionVersion.Should().Be(2);
    }

    [Fact]
    public void Given_Artist_Discovered_When_Applied_Then_Search_Text_And_Artist_Metadata_Are_Repaired()
    {
        var projection = MusicTrackProjection.Load(
            new MusicTrackProjectionSnapshot(
                null,
                null,
                "Song A",
                ArtistName.From("Wrong Artist"),
                AlbumTitle.Empty,
                ArtistName.From("Song A Wrong Artist").Normalized,
                null,
                string.Empty,
                null,
                string.Empty,
                null,
                null,
                null,
                null,
                null,
                null,
                null,
                null,
                false,
                0));

        projection.Apply(
            new ArtistDiscovered("artist_a", "Artist A", "mb-artist-a", ProviderName.MusicBrainz, Clock),
            3);

        projection.ArtistId.Should().Be("artist_a");
        projection.Artist.Value.Should().Be("Artist A");
        projection.Artist.Normalized.Should().Be("artist a");
        projection.SearchText.Should().Be(ArtistName.From("Song A Artist A").Normalized);
        projection.ProjectionVersion.Should().Be(3);
    }

    [Fact]
    public void Given_Metadata_Corrected_When_Applied_Then_All_Resolved_Metadata_Fields_Are_Replaced()
    {
        var projection = new MusicTrackProjection();

        projection.Apply(
            new MetadataCorrected(
                "Song A (Remastered)",
                "Artist A",
                "artist_a",
                "mb-artist-a",
                "Album A",
                "album_a",
                "mb-release-a",
                new DateOnly(2004, 6, 7),
                123000,
                "isrc-1",
                "mbid-1",
                "admin/repair",
                Clock),
            4);

        projection.Title.Should().Be("Song A (Remastered)");
        projection.Artist.Value.Should().Be("Artist A");
        projection.ArtistId.Should().Be("artist_a");
        projection.AlbumTitle.Value.Should().Be("Album A");
        projection.AlbumTitle.Normalized.Should().Be("album a");
        projection.AlbumId.Should().Be("album_a");
        projection.ReleaseDate.Should().Be(new DateOnly(2004, 6, 7));
        projection.Isrc.Should().Be("isrc-1");
        projection.Mbid.Should().Be("mbid-1");
        projection.ResolvedMetadata.Should().NotBeNull();
        projection.ResolvedMetadata!.Title.Should().Be("Song A (Remastered)");
        projection.ResolvedMetadata.Artist.Value.Should().Be("Artist A");
        projection.ProjectionVersion.Should().Be(4);
    }

    [Fact]
    public void Given_Album_Discovered_When_Applied_Then_Release_Date_Is_Recorded()
    {
        var projection = new MusicTrackProjection();

        projection.Apply(
            new AlbumDiscovered(
                "album_a",
                "Album A",
                "mb-release-a",
                new DateOnly(2004, 6, 7),
                ProviderName.MusicBrainz,
                Clock),
            2);

        projection.AlbumId.Should().Be("album_a");
        projection.AlbumTitle.Value.Should().Be("Album A");
        projection.ReleaseDate.Should().Be(new DateOnly(2004, 6, 7));
        projection.ProjectionVersion.Should().Be(2);
    }

    [Fact]
    public void Given_Track_Artwork_When_Applied_Then_Only_Track_Artwork_Is_Stored()
    {
        var projection = new MusicTrackProjection();

        projection.Apply(
            new ArtworkDiscovered(
                CatalogEntityKind.Track,
                null,
                new Uri("https://images.example.com/track.png"),
                "worker/musicbrainz",
                Clock),
            5);

        projection.ArtworkUrl.Should().Be("https://images.example.com/track.png");
        projection.ProjectionVersion.Should().Be(5);
    }

    [Fact]
    public void Given_Spotify_Only_Without_Resolved_Metadata_When_Applied_Then_The_Track_Is_Not_Playable()
    {
        var projection = new MusicTrackProjection();

        projection.Apply(
            new ProviderReferenceDiscovered(
                ProviderName.Spotify,
                "spotify-1",
                new Uri("https://open.spotify.com/track/spotify-1"),
                ProviderName.Odesli,
                Clock),
            1);

        projection.SpotifyId.Should().Be("spotify-1");
        projection.IsPlayable.Should().BeFalse();
    }

    private static readonly DateTimeOffset Clock = new(2026, 6, 18, 12, 0, 0, TimeSpan.Zero);
}
