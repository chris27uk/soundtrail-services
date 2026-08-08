using Soundtrail.Services.Api.Features.Catalog.GetTracksForPlaylist.Contract;

namespace Soundtrail.Services.Tests.Unit.Sociable.Features.GetTracksForPlaylist.Scenarios.LookupDataNotComplete.Api;

public sealed class LookupDataNotCompleteTests
{
    [Fact]
    public async Task Given_A_Request_Is_Being_Orchestrated_When_Requesting_Then_No_Tracks_Are_Returned()
    {
        var environment = await GetTracksForPlaylistSociableTestEnvironment.ForExistingIncompleteLookup();

        var response = await environment.ProjectOnChange(
            sut => sut.Handle(new GetTracksForPlaylistRequest(environment.PlaylistId)));

        response!.Tracks.Should().BeEmpty();
    }

    [Fact]
    public async Task Given_A_Request_Is_Being_Orchestrated_When_Requesting_Then_Discovery_Remains_Scheduled()
    {
        var environment = await GetTracksForPlaylistSociableTestEnvironment.ForExistingIncompleteLookup();

        var response = await environment.ProjectOnChange(
            sut => sut.Handle(new GetTracksForPlaylistRequest(environment.PlaylistId)));

        response!.Discovery!.Status.Should().Be("scheduled");
    }
}
