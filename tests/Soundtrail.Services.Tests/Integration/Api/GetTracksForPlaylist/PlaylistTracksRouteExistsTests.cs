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
    public async Task Given_Existing_Playlist_Tracks_When_Requesting_The_Playlist_Tracks_Then_Discovery_Response_Headers_Are_Returned()
    {
        using var environment = GetTracksForPlaylistRouteTestEnvironment.ForExistingPlaylistTracks();

        var response = await environment.Client.GetAsync("/catalog/playlists/worldwidesongchart/tracks");

        response.Headers.CacheControl.Should().NotBeNull();
        response.Headers.CacheControl!.NoStore.Should().BeTrue();
        response.Headers.RetryAfter.Should().NotBeNull();
        response.Headers.RetryAfter!.Delta.Should().NotBeNull();
        response.Headers.RetryAfter.Delta!.Value.TotalSeconds.Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task Given_Existing_Playlist_Tracks_When_Response_Contains_Null_Values_Then_Nulls_Are_Not_Serialized()
    {
        using var environment = GetTracksForPlaylistRouteTestEnvironment.ForExistingPlaylistTracks();

        var response = await environment.Client.GetAsync("/catalog/playlists/worldwidesongchart/tracks");
        var body = await response.Content.ReadAsStringAsync();

        body.Should().NotContain("\"isrc\"");
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

    [Fact]
    public async Task Given_Missing_Playlist_Tracks_When_Response_Discovery_Is_Null_Then_Discovery_Is_Not_Serialized()
    {
        using var environment = GetTracksForPlaylistRouteTestEnvironment.ForMissingPlaylistTracks();

        var response = await environment.Client.GetAsync("/catalog/playlists/unknownplaylist/tracks");
        var body = await response.Content.ReadAsStringAsync();

        body.Should().NotContain("\"discovery\"");
    }

    [Fact]
    public async Task Given_Missing_Playlist_Tracks_When_Requesting_The_Playlist_Tracks_Then_No_Store_Header_Is_Returned()
    {
        using var environment = GetTracksForPlaylistRouteTestEnvironment.ForMissingPlaylistTracks();

        var response = await environment.Client.GetAsync("/catalog/playlists/unknownplaylist/tracks");

        response.Headers.CacheControl.Should().NotBeNull();
        response.Headers.CacheControl!.NoStore.Should().BeTrue();
        response.Headers.RetryAfter.Should().BeNull();
    }
}
