using Soundtrail.Services.Api.Features.Catalog.GetTracksForPlaylist.Contract;

namespace Soundtrail.Services.Tests.Unit.Sociable.GetTracksForPlaylist.CompletedEmptyProjection;

public sealed class CompletedEmptyProjectionTests
{
    [Fact]
    public async Task Given_A_Completed_Playlist_Projection_With_No_Tracks_When_Requesting_Then_Catch_Up_Is_Scheduled()
    {
        var environment = GetTracksForPlaylistSociableTestEnvironment.ForCompletedEmptyProjection();

        var response = await environment.ProjectOnChange(
            sut => sut.Handle(new GetTracksForPlaylistRequest(environment.PlaylistId)));

        response!.Discovery!.Reason.Should().Be("Playlist projection is still catching up.");
    }

    [Fact]
    public async Task Given_A_Completed_Playlist_Projection_With_No_Tracks_When_Requesting_Then_Discovery_Is_Scheduled()
    {
        var environment = GetTracksForPlaylistSociableTestEnvironment.ForCompletedEmptyProjection();

        var response = await environment.ProjectOnChange(
            sut => sut.Handle(new GetTracksForPlaylistRequest(environment.PlaylistId)));

        response!.Discovery!.Status.Should().Be("scheduled");
    }
}
