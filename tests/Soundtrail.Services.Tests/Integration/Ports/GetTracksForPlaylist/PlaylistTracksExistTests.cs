using Soundtrail.Domain.Catalog;
using Soundtrail.Domain.Catalog.Playlists;
using Soundtrail.Domain.Catalog.Tracks;

namespace Soundtrail.Services.Tests.Integration.Ports.GetTracksForPlaylist;

public sealed class PlaylistTracksExistTests
{
    public static TheoryData<GetTracksForPlaylistPortImplementation> Implementations => new()
    {
        GetTracksForPlaylistPortImplementation.Fake,
        GetTracksForPlaylistPortImplementation.Raven
    };

    [Theory]
    [MemberData(nameof(Implementations))]
    public async Task Given_Existing_Playlist_Tracks_When_Requesting_The_Playlist_Tracks_Then_Playlist_Tracks_Are_Returned(GetTracksForPlaylistPortImplementation implementation)
    {
        await using var environment = await GetTracksForPlaylistPortContractTestEnvironment.ForExistingPlaylistTracks(implementation);

        var result = await environment.Subject.GetTracksForPlaylistAsync(environment.PlaylistId, CancellationToken.None);

        result.Should().NotBeNull();
    }

    [Theory]
    [MemberData(nameof(Implementations))]
    public async Task Given_Existing_Playlist_Tracks_When_Requesting_The_Playlist_Tracks_Then_The_Playlist_Id_Is_Returned(GetTracksForPlaylistPortImplementation implementation)
    {
        await using var environment = await GetTracksForPlaylistPortContractTestEnvironment.ForExistingPlaylistTracks(implementation, playlistName: "RoadTrip");

        var result = await environment.Subject.GetTracksForPlaylistAsync(environment.PlaylistId, CancellationToken.None);

        result!.PlaylistId.Should().Be(PlaylistId.FromPlaylistName("RoadTrip"));
    }

    [Theory]
    [MemberData(nameof(Implementations))]
    public async Task Given_Existing_Playlist_Tracks_When_Requesting_The_Playlist_Tracks_Then_The_Tracks_Are_Returned(GetTracksForPlaylistPortImplementation implementation)
    {
        await using var environment = await GetTracksForPlaylistPortContractTestEnvironment.ForExistingPlaylistTracks(implementation);

        var result = await environment.Subject.GetTracksForPlaylistAsync(environment.PlaylistId, CancellationToken.None);

        result!.Tracks.Should().HaveCount(1);
    }

    [Theory]
    [MemberData(nameof(Implementations))]
    public async Task Given_Existing_Playlist_Tracks_When_Requesting_The_Playlist_Tracks_Then_The_Track_Id_Is_Returned(GetTracksForPlaylistPortImplementation implementation)
    {
        await using var environment = await GetTracksForPlaylistPortContractTestEnvironment.ForExistingPlaylistTracks(implementation, trackId: global::Soundtrail.Services.Tests.TestTrackIds.Value("track-3603"));

        var result = await environment.Subject.GetTracksForPlaylistAsync(environment.PlaylistId, CancellationToken.None);

        result!.Tracks[0].TrackId.Should().Be(global::Soundtrail.Services.Tests.TestTrackIds.Create("track-3603"));
    }

    [Theory]
    [MemberData(nameof(Implementations))]
    public async Task Given_Existing_Playlist_Tracks_When_Requesting_The_Playlist_Tracks_Then_The_Music_Catalog_Id_Is_Returned(GetTracksForPlaylistPortImplementation implementation)
    {
        await using var environment = await GetTracksForPlaylistPortContractTestEnvironment.ForExistingPlaylistTracks(implementation, trackId: global::Soundtrail.Services.Tests.TestTrackIds.Value("track-3604"));

        var result = await environment.Subject.GetTracksForPlaylistAsync(environment.PlaylistId, CancellationToken.None);

        result!.Tracks[0].MusicCatalogId.Should().Be(new CatalogItemId.Track(global::Soundtrail.Services.Tests.TestTrackIds.Create("track-3604")));
    }

    [Theory]
    [MemberData(nameof(Implementations))]
    public async Task Given_Existing_Playlist_Tracks_When_Requesting_The_Playlist_Tracks_Then_The_Track_Title_Is_Returned(GetTracksForPlaylistPortImplementation implementation)
    {
        await using var environment = await GetTracksForPlaylistPortContractTestEnvironment.ForExistingPlaylistTracks(implementation, title: "Track 3605");

        var result = await environment.Subject.GetTracksForPlaylistAsync(environment.PlaylistId, CancellationToken.None);

        result!.Tracks[0].Title.Should().Be("Track 3605");
    }

    [Theory]
    [MemberData(nameof(Implementations))]
    public async Task Given_Existing_Playlist_Tracks_When_Requesting_The_Playlist_Tracks_Then_The_Track_Artist_Name_Is_Returned(GetTracksForPlaylistPortImplementation implementation)
    {
        await using var environment = await GetTracksForPlaylistPortContractTestEnvironment.ForExistingPlaylistTracks(implementation, artistName: "Artist 3606");

        var result = await environment.Subject.GetTracksForPlaylistAsync(environment.PlaylistId, CancellationToken.None);

        result!.Tracks[0].ArtistName.Should().Be("Artist 3606");
    }

    [Theory]
    [MemberData(nameof(Implementations))]
    public async Task Given_Existing_Playlist_Tracks_When_Requesting_The_Playlist_Tracks_Then_The_Album_Title_Is_Returned(GetTracksForPlaylistPortImplementation implementation)
    {
        await using var environment = await GetTracksForPlaylistPortContractTestEnvironment.ForExistingPlaylistTracks(implementation, albumTitle: "Album 3607");

        var result = await environment.Subject.GetTracksForPlaylistAsync(environment.PlaylistId, CancellationToken.None);

        result!.Tracks[0].AlbumTitle.Should().Be("Album 3607");
    }

    [Theory]
    [MemberData(nameof(Implementations))]
    public async Task Given_Existing_Playlist_Tracks_When_Requesting_The_Playlist_Tracks_Then_The_Track_Duration_Is_Returned(GetTracksForPlaylistPortImplementation implementation)
    {
        await using var environment = await GetTracksForPlaylistPortContractTestEnvironment.ForExistingPlaylistTracks(implementation, durationMs: 207000);

        var result = await environment.Subject.GetTracksForPlaylistAsync(environment.PlaylistId, CancellationToken.None);

        result!.Tracks[0].DurationMs.Should().Be(207000);
    }

    [Theory]
    [MemberData(nameof(Implementations))]
    public async Task Given_Existing_Playlist_Tracks_When_Requesting_The_Playlist_Tracks_Then_The_Track_Isrc_Is_Returned(GetTracksForPlaylistPortImplementation implementation)
    {
        await using var environment = await GetTracksForPlaylistPortContractTestEnvironment.ForExistingPlaylistTracks(implementation, isrc: "GBAYE2403609");

        var result = await environment.Subject.GetTracksForPlaylistAsync(environment.PlaylistId, CancellationToken.None);

        result!.Tracks[0].Isrc.Should().Be("GBAYE2403609");
    }

    [Theory]
    [MemberData(nameof(Implementations))]
    public async Task Given_Existing_Playlist_Tracks_When_Requesting_The_Playlist_Tracks_Then_The_Track_Release_Date_Is_Returned(GetTracksForPlaylistPortImplementation implementation)
    {
        var releaseDate = new DateOnly(2024, 11, 12);
        await using var environment = await GetTracksForPlaylistPortContractTestEnvironment.ForExistingPlaylistTracks(implementation, releaseDate: releaseDate);

        var result = await environment.Subject.GetTracksForPlaylistAsync(environment.PlaylistId, CancellationToken.None);

        result!.Tracks[0].ReleaseDate.Should().Be(releaseDate);
    }

    [Theory]
    [MemberData(nameof(Implementations))]
    public async Task Given_Existing_Playlist_Tracks_When_Requesting_The_Playlist_Tracks_Then_The_Track_Artwork_Url_Is_Returned(GetTracksForPlaylistPortImplementation implementation)
    {
        await using var environment = await GetTracksForPlaylistPortContractTestEnvironment.ForExistingPlaylistTracks(implementation, artworkUrl: "https://cdn.soundtrail.test/tracks/track-3611.jpg");

        var result = await environment.Subject.GetTracksForPlaylistAsync(environment.PlaylistId, CancellationToken.None);

        result!.Tracks[0].ArtworkUrl.Should().Be("https://cdn.soundtrail.test/tracks/track-3611.jpg");
    }
}
