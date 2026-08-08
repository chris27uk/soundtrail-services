using Soundtrail.Domain.Discovery.Messages;
using Soundtrail.Services.Api.Features.Catalog.GetTracksForPlaylist;
using Soundtrail.Services.Api.Features.Catalog.GetTracksForPlaylist.Contract;

namespace Soundtrail.Services.Tests.Unit.Sociable.GetTracksForPlaylist.LookupDataNotComplete;

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

    [Fact]
    public async Task Given_A_Request_Is_Being_Orchestrated_Then_Assessment_Is_Requested()
    {
        var environment = GetTracksForPlaylistSociableTestEnvironment.ForLookupDataNotComplete();

        await environment.ProjectOnChange(
            sut => sut.Handle(new GetTracksForPlaylistRequest(environment.PlaylistId)));

        environment.SentMessage<AssessWorkMessage>().Target.NormalisedIdentifier.Should()
            .Be($"child_tracks_for_playlist:{environment.PlaylistId.Value}");
    }
}
