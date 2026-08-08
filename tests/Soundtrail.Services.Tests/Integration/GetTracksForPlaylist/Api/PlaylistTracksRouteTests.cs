using System.Net;
using System.Net.Http.Json;
using Soundtrail.Services.Api.Features.Catalog.GetTracksForPlaylist.Adapters;

namespace Soundtrail.Services.Tests.Integration.GetTracksForPlaylist.Api;

public sealed class PlaylistTracksRouteTests
{
    [Fact]
    public async Task Given_No_Local_Playlist_Projection_When_Requesting_Tracks_Then_Ok_Is_Returned()
    {
        await using var environment = await GetTracksForPlaylistApiTestEnvironment.ForCatchingUpAsync();

        var response = await environment.Client.GetAsync(
            $"/catalog/playlists/{environment.PlaylistId.Value}/tracks");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Given_Discovery_Present_When_Requesting_Tracks_Then_Discovery_Is_Deserialized()
    {
        await using var environment = await GetTracksForPlaylistApiTestEnvironment.ForDiscoveryPresentAsync();

        var body = await environment.GetPlaylistAsync();

        body.Should().NotBeNull();
        body!.Discovery.Should().NotBeNull();
        body.Discovery!.Status.Should().Be("scheduled");
        body.Tracks.Should().BeEmpty();
    }

    [Fact]
    public async Task Given_Lookup_Complete_When_Requesting_Tracks_Then_Tracks_And_Discovery_Are_Deserialized()
    {
        await using var environment = await GetTracksForPlaylistApiTestEnvironment.ForLookupCompleteAsync();

        var response = await environment.Client.GetAsync(
            $"/catalog/playlists/{environment.PlaylistId.Value}/tracks");
        var body = await response.Content.ReadFromJsonAsync<GetTracksForPlaylistResponseDto>();
        var raw = await response.Content.ReadAsStringAsync();

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        body.Should().NotBeNull();
        body!.Tracks.Should().ContainSingle();
        body.Tracks[0].Title.Should().Be("Midnight Signals");
        body.Discovery.Should().NotBeNull();
        body.Discovery!.Status.Should().Be("completed");
        raw.Should().NotContain("\"isrc\"");
    }

    [Fact]
    public async Task Given_Playlist_Tracks_Port_Fails_When_Requesting_Tracks_Then_Internal_Server_Error_Is_Returned()
    {
        await using var environment = await GetTracksForPlaylistApiTestEnvironment.ForPortFailureAsync();

        var response = await environment.Client.GetAsync(
            $"/catalog/playlists/{environment.PlaylistId.Value}/tracks");

        response.StatusCode.Should().Be(HttpStatusCode.InternalServerError);
    }
}
