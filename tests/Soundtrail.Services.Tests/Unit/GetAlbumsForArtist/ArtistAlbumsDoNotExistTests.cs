using Soundtrail.Domain.Catalog;
using Soundtrail.Domain.Catalog.Artists;

namespace Soundtrail.Services.Tests.Unit.GetAlbumsForArtist;

public sealed class ArtistAlbumsDoNotExistTests
{
    [Fact]
    public async Task Given_Missing_Artist_Albums_When_Requesting_The_Artist_Albums_Then_No_Artist_Albums_Are_Returned()
    {
        var environment = GetAlbumsForArtistMissingUnitTestEnvironment.ForMissingArtistAlbums();

        var result = await environment.CreateSubjectUnderTest().Handle(environment.CreateRequest());

        result.Should().BeNull();
    }

    [Fact]
    public async Task Given_Missing_Artist_Albums_When_Requesting_The_Artist_Albums_Then_The_Requested_Artist_Id_Is_Read()
    {
        var artistId = ArtistId.From("artist-1707");
        var environment = GetAlbumsForArtistMissingUnitTestEnvironment.ForMissingArtistAlbums(artistId);

        await environment.CreateSubjectUnderTest().Handle(environment.CreateRequest());

        environment.Port.RequestedArtistIds.Single().Should().Be(artistId);
    }
}
