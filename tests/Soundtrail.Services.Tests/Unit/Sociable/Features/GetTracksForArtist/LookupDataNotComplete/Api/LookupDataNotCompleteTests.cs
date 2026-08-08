using Soundtrail.Services.Api.Features.Catalog.GetTracksForArtist.Contract;

namespace Soundtrail.Services.Tests.Unit.Sociable.GetTracksForArtist.LookupDataNotComplete.Api;

public sealed class LookupDataNotCompleteTests
{
    [Fact]
    public async Task Given_A_Request_Is_Being_Orchestrated_When_Requesting_Then_No_Artist_Tracks_Are_Returned()
    {
        var environment = await GetTracksForArtistSociableTestEnvironment.ForExistingIncompleteLookup();

        var response = await environment.ProjectOnChange(
            sut => sut.Handle(new GetTracksForArtistRequest(environment.ArtistId)));

        response.Should().BeNull();
    }
}
