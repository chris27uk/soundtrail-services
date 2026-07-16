using Soundtrail.Contracts.Common;
using Soundtrail.Domain.Catalog;
using Soundtrail.Domain.Catalog.Playlists;
using Soundtrail.Domain.Catalog.Tracks;
using Soundtrail.Domain.Common;
using Soundtrail.Domain.Discovery;
using Soundtrail.Domain.Discovery.Events;

namespace Soundtrail.Services.Tests.Unit.GetTracksForPlaylist;

public sealed class PlaylistTracksExistTests
{
    [Fact]
    public async Task Given_Existing_Playlist_Tracks_When_Requesting_The_Playlist_Tracks_Then_The_Port_Response_Is_Returned()
    {
        var response = PlaylistTracks.CreateResponse();
        var environment = GetTracksForPlaylistUnitTestEnvironment.ForExistingPlaylistTracks(response: response);

        var result = await environment.CreateSubjectUnderTest().Handle(environment.CreateRequest());

        result.Should().BeSameAs(response);
    }

    [Fact]
    public async Task Given_Existing_Playlist_Tracks_When_Requesting_The_Playlist_Tracks_Then_The_Playlist_Id_Is_Returned()
    {
        var playlistId = PlaylistId.FromPlaylistName("RoadTrip");
        var environment = GetTracksForPlaylistUnitTestEnvironment.ForExistingPlaylistTracks(playlistId: playlistId);

        var result = await environment.CreateSubjectUnderTest().Handle(environment.CreateRequest());

        result!.PlaylistId.Should().Be(playlistId);
    }

    [Fact]
    public async Task Given_Existing_Playlist_Tracks_When_Requesting_The_Playlist_Tracks_Then_The_Tracks_Are_Returned()
    {
        var environment = GetTracksForPlaylistUnitTestEnvironment.ForExistingPlaylistTracks();

        var result = await environment.CreateSubjectUnderTest().Handle(environment.CreateRequest());

        result!.Tracks.Should().HaveCount(1);
    }

    [Fact]
    public async Task Given_Existing_Playlist_Tracks_When_Requesting_The_Playlist_Tracks_Then_The_Track_Id_Is_Returned()
    {
        var trackId = TrackId.From("track-3303");
        var environment = GetTracksForPlaylistUnitTestEnvironment.ForExistingPlaylistTracks(
            response: PlaylistTracks.CreateResponse(trackId: trackId));

        var result = await environment.CreateSubjectUnderTest().Handle(environment.CreateRequest());

        result!.Tracks[0].TrackId.Should().Be(trackId);
    }

    [Fact]
    public async Task Given_Existing_Playlist_Tracks_When_Requesting_The_Playlist_Tracks_Then_The_Music_Catalog_Id_Is_Returned()
    {
        var trackId = TrackId.From("track-3304");
        var environment = GetTracksForPlaylistUnitTestEnvironment.ForExistingPlaylistTracks(
            response: PlaylistTracks.CreateResponse(trackId: trackId));

        var result = await environment.CreateSubjectUnderTest().Handle(environment.CreateRequest());

        result!.Tracks[0].MusicCatalogId.Should().Be(new CatalogItemId.Track(trackId));
    }

    [Fact]
    public async Task Given_Existing_Playlist_Tracks_When_Requesting_The_Playlist_Tracks_Then_The_Track_Title_Is_Returned()
    {
        var environment = GetTracksForPlaylistUnitTestEnvironment.ForExistingPlaylistTracks(
            response: PlaylistTracks.CreateResponse(title: "Track 3305"));

        var result = await environment.CreateSubjectUnderTest().Handle(environment.CreateRequest());

        result!.Tracks[0].Title.Should().Be("Track 3305");
    }

    [Fact]
    public async Task Given_Existing_Playlist_Tracks_When_Requesting_The_Playlist_Tracks_Then_The_Track_Artist_Name_Is_Returned()
    {
        var environment = GetTracksForPlaylistUnitTestEnvironment.ForExistingPlaylistTracks(
            response: PlaylistTracks.CreateResponse(artistName: "Artist 3306"));

        var result = await environment.CreateSubjectUnderTest().Handle(environment.CreateRequest());

        result!.Tracks[0].ArtistName.Should().Be("Artist 3306");
    }

    [Fact]
    public async Task Given_Existing_Playlist_Tracks_When_Requesting_The_Playlist_Tracks_Then_The_Album_Title_Is_Returned()
    {
        var environment = GetTracksForPlaylistUnitTestEnvironment.ForExistingPlaylistTracks(
            response: PlaylistTracks.CreateResponse(albumTitle: "Album 3307"));

        var result = await environment.CreateSubjectUnderTest().Handle(environment.CreateRequest());

        result!.Tracks[0].AlbumTitle.Should().Be("Album 3307");
    }

    [Fact]
    public async Task Given_Existing_Playlist_Tracks_When_Requesting_The_Playlist_Tracks_Then_The_Track_Duration_Is_Returned()
    {
        var environment = GetTracksForPlaylistUnitTestEnvironment.ForExistingPlaylistTracks(
            response: PlaylistTracks.CreateResponse(durationMs: 208000));

        var result = await environment.CreateSubjectUnderTest().Handle(environment.CreateRequest());

        result!.Tracks[0].DurationMs.Should().Be(208000);
    }

    [Fact]
    public async Task Given_Existing_Playlist_Tracks_When_Requesting_The_Playlist_Tracks_Then_The_Track_Isrc_Is_Returned()
    {
        var environment = GetTracksForPlaylistUnitTestEnvironment.ForExistingPlaylistTracks(
            response: PlaylistTracks.CreateResponse(isrc: "GBAYE2403309"));

        var result = await environment.CreateSubjectUnderTest().Handle(environment.CreateRequest());

        result!.Tracks[0].Isrc.Should().Be("GBAYE2403309");
    }

    [Fact]
    public async Task Given_Existing_Playlist_Tracks_When_Requesting_The_Playlist_Tracks_Then_The_Track_Release_Date_Is_Returned()
    {
        var releaseDate = new DateOnly(2024, 11, 12);
        var environment = GetTracksForPlaylistUnitTestEnvironment.ForExistingPlaylistTracks(
            response: PlaylistTracks.CreateResponse(releaseDate: releaseDate));

        var result = await environment.CreateSubjectUnderTest().Handle(environment.CreateRequest());

        result!.Tracks[0].ReleaseDate.Should().Be(releaseDate);
    }

    [Fact]
    public async Task Given_Existing_Playlist_Tracks_When_Requesting_The_Playlist_Tracks_Then_The_Track_Artwork_Url_Is_Returned()
    {
        var environment = GetTracksForPlaylistUnitTestEnvironment.ForExistingPlaylistTracks(
            response: PlaylistTracks.CreateResponse(artworkUrl: "https://cdn.soundtrail.test/tracks/track-3311.jpg"));

        var result = await environment.CreateSubjectUnderTest().Handle(environment.CreateRequest());

        result!.Tracks[0].ArtworkUrl.Should().Be("https://cdn.soundtrail.test/tracks/track-3311.jpg");
    }

    [Fact]
    public async Task Given_Existing_Playlist_Tracks_When_Requesting_The_Playlist_Tracks_Then_The_Requested_Playlist_Id_Is_Read()
    {
        var playlistId = PlaylistId.FromPlaylistName("FocusedMix");
        var environment = GetTracksForPlaylistUnitTestEnvironment.ForExistingPlaylistTracks(playlistId: playlistId);

        await environment.CreateSubjectUnderTest().Handle(environment.CreateRequest());

        environment.Port.RequestedPlaylistIds.Single().Should().Be(playlistId);
    }

    [Fact]
    public async Task Given_Existing_Playlist_Tracks_When_Requesting_The_Playlist_Tracks_Then_A_Search_Command_Is_Sent()
    {
        var environment = GetTracksForPlaylistUnitTestEnvironment.ForExistingPlaylistTracks();

        await environment.CreateSubjectUnderTest().Handle(environment.CreateRequest());

        environment.CommandBus.Commands.Should().ContainSingle();
    }

    [Fact]
    public async Task Given_Existing_Playlist_Tracks_When_Requesting_The_Playlist_Tracks_Then_The_Search_Command_Filter_Is_Playlist_Based()
    {
        var playlistId = PlaylistId.FromPlaylistName("WorkoutMix");
        var environment = GetTracksForPlaylistUnitTestEnvironment.ForExistingPlaylistTracks(playlistId: playlistId);

        await environment.CreateSubjectUnderTest().Handle(environment.CreateRequest());

        environment.CommandBus.Commands.Single().Target.Should().Be(new EnrichmentTarget.Existing(new CatalogItemId.Playlist(playlistId)));
    }

    [Fact]
    public async Task Given_Existing_Playlist_Tracks_When_Requesting_The_Playlist_Tracks_Then_The_Search_Command_Requests_Tracks()
    {
        var environment = GetTracksForPlaylistUnitTestEnvironment.ForExistingPlaylistTracks();

        await environment.CreateSubjectUnderTest().Handle(environment.CreateRequest());

        environment.CommandBus.Commands.Single().RequiredCatalogType.Should().Be(RequiredCatalogType.Tracks);
    }

    [Fact]
    public async Task Given_Existing_Playlist_Tracks_When_Requesting_The_Playlist_Tracks_Then_The_Search_Command_Has_High_Priority()
    {
        var environment = GetTracksForPlaylistUnitTestEnvironment.ForExistingPlaylistTracks();

        await environment.CreateSubjectUnderTest().Handle(environment.CreateRequest());

        environment.CommandBus.Commands.Single().Priority.Should().Be(LookupPriorityBand.High);
    }

    [Fact]
    public async Task Given_Existing_Playlist_Tracks_When_Requesting_The_Playlist_Tracks_Then_The_Search_Command_Trust_Level_Is_One_Hundred()
    {
        var environment = GetTracksForPlaylistUnitTestEnvironment.ForExistingPlaylistTracks();

        await environment.CreateSubjectUnderTest().Handle(environment.CreateRequest());

        environment.CommandBus.Commands.Single().TrustLevel.Should().Be(100);
    }

    [Fact]
    public async Task Given_Existing_Playlist_Tracks_When_Requesting_The_Playlist_Tracks_Then_The_Search_Command_Risk_Score_Is_Zero()
    {
        var environment = GetTracksForPlaylistUnitTestEnvironment.ForExistingPlaylistTracks();

        await environment.CreateSubjectUnderTest().Handle(environment.CreateRequest());

        environment.CommandBus.Commands.Single().RiskScore.Should().Be(0);
    }

    [Fact]
    public async Task Given_Existing_Playlist_Tracks_When_Requesting_The_Playlist_Tracks_Then_The_Search_Command_Requested_At_Is_Set()
    {
        var environment = GetTracksForPlaylistUnitTestEnvironment.ForExistingPlaylistTracks();

        await environment.CreateSubjectUnderTest().Handle(environment.CreateRequest());

        environment.CommandBus.Commands.Single().RequestedAt.Should().Be(environment.Clock.UtcNow);
    }
}
