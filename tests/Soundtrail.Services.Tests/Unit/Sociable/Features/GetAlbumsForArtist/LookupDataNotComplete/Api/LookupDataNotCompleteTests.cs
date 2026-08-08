using Soundtrail.Services.Api.Features.Catalog.GetAlbumsForArtist.Contract;

namespace Soundtrail.Services.Tests.Unit.Sociable.GetAlbumsForArtist.LookupDataNotComplete.Api;

public sealed class LookupDataNotCompleteTests
{
    [Fact]
    public async Task Given_A_Request_Is_Being_Orchestrated_When_Requesting_Then_No_Artist_Albums_Are_Returned()
    {
        var environment = await GetAlbumsForArtistSociableTestEnvironment.ForExistingIncompleteLookup();

        var response = await environment.ProjectOnChange(
            sut => sut.Handle(new GetAlbumsForArtistRequest(environment.ArtistId)));

        response.Should().BeNull();
    }
}
