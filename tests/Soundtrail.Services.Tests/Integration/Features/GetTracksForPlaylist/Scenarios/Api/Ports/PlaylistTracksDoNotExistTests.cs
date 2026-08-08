using Soundtrail.Domain.Catalog.Playlists;
using Soundtrail.Services.Tests.Integration.Features.GetTracksForPlaylist;

namespace Soundtrail.Services.Tests.Integration.Features.GetTracksForPlaylist.Scenarios.Api.Ports;

public sealed class PlaylistTracksDoNotExistTests
{
    public static TheoryData<GetTracksForPlaylistPortImplementation> Implementations => new()
    {
        GetTracksForPlaylistPortImplementation.Raven
    };

    [Theory]
    [MemberData(nameof(Implementations))]
    public async Task Given_Missing_Playlist_Tracks_When_Requesting_The_Playlist_Tracks_Then_No_Playlist_Tracks_Are_Returned(GetTracksForPlaylistPortImplementation implementation)
    {
        await using var environment = await GetTracksForPlaylistPortContractTestEnvironment.ForMissingPlaylistTracks(
            implementation,
            PlaylistId.FromPlaylistName("UnknownPlaylist"));

        var result = await environment.Subject.GetTracksForPlaylistAsync(environment.PlaylistId, CancellationToken.None);

        result.Should().BeNull();
    }
}
