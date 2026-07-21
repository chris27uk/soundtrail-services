using System.Net;

namespace Soundtrail.Services.Tests.Integration.Api.GetTracksForPlaylist;

public sealed class PlaylistTracksRouteExistsTests
{
    [Fact]
    public async Task Given_Existing_Playlist_Tracks_When_Requesting_The_Playlist_Tracks_Then_Ok_Is_Returned()
    {
        using var environment = GetTracksForPlaylistRouteTestEnvironment.ForExistingPlaylistTracks();

        var response = await environment.Client.GetAsync("/catalog/playlists/worldwidesongchart/tracks");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }
}
