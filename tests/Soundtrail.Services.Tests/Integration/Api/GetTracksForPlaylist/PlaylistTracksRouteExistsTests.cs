using System.Net;
using System.Net.Http.Json;
using Soundtrail.Services.Api.Features.Catalog.GetTracksForPlaylist.Adapters;

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

    [Fact]
    public async Task Given_Missing_Playlist_Tracks_When_Requesting_The_Playlist_Tracks_Then_NotFound_With_A_Response_Body_Is_Returned()
    {
        using var environment = GetTracksForPlaylistRouteTestEnvironment.ForMissingPlaylistTracks();

        var response = await environment.Client.GetAsync("/catalog/playlists/unknownplaylist/tracks");
        var body = await response.Content.ReadFromJsonAsync<GetTracksForPlaylistResponseDto>();

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        body.Should().NotBeNull();
        body!.PlaylistId.Should().Be("unknownplaylist");
        body.Tracks.Should().BeEmpty();
    }
}
