using Soundtrail.Domain.Catalog.Playlists;

namespace Soundtrail.Services.Tests.Unit.GetTracksForPlaylist;

public sealed class PlaylistTracksDoNotExistTests
{
    [Fact]
    public async Task Given_Missing_Playlist_Tracks_When_Requesting_The_Playlist_Tracks_Then_No_Playlist_Tracks_Are_Returned()
    {
        var playlistId = PlaylistId.FromPlaylistName("UnknownPlaylist");
        var environment = GetTracksForPlaylistMissingUnitTestEnvironment.ForMissingPlaylistTracks(playlistId);

        var result = await environment.CreateSubjectUnderTest().Handle(environment.CreateRequest());

        result.Should().BeNull();
    }
}
