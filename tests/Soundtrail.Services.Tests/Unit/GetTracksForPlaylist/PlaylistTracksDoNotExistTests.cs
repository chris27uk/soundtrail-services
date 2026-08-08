using Soundtrail.Domain.Catalog.Playlists;
using Soundtrail.Domain.Common;
using Soundtrail.Services.Api.Features.Catalog.Shared.Contract;

namespace Soundtrail.Services.Tests.Unit.GetTracksForPlaylist;

public sealed class PlaylistTracksDoNotExistTests
{
    [Fact]
    public async Task Given_Missing_Playlist_Tracks_When_Requesting_The_Playlist_Tracks_Then_No_Playlist_Tracks_Are_Returned()
    {
        var playlistId = PlaylistId.FromPlaylistName("UnknownPlaylist");
        var environment = GetTracksForPlaylistUnitTestEnvironment.ForMissingPlaylistTracks(playlistId);
        var sut = environment.CreateSubjectUnderTest();

        var result = await sut.Handle(environment.CreateRequest());

        result!.PlaylistId.Should().Be(playlistId);
    }

}
