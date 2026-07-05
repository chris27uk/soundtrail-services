using Soundtrail.Domain.Catalog;

namespace Soundtrail.Services.Tests.Unit.GetAlbum;

public sealed class AlbumDoesNotExistTests
{
    [Fact]
    public async Task Given_A_Missing_Album_When_Requesting_The_Album_Then_No_Album_Is_Returned()
    {
        var environment = GetAlbumMissingUnitTestEnvironment.ForMissingAlbum();

        var result = await environment.CreateSubjectUnderTest().Handle(environment.CreateRequest());

        result.Should().BeNull();
    }

    [Fact]
    public async Task Given_A_Missing_Album_When_Requesting_The_Album_Then_The_Requested_Album_Id_Is_Read()
    {
        var albumId = AlbumId.From("artist-203", "album-403");
        var environment = GetAlbumMissingUnitTestEnvironment.ForMissingAlbum(albumId);

        await environment.CreateSubjectUnderTest().Handle(environment.CreateRequest());

        environment.Port.RequestedAlbumIds.Single().Should().Be(albumId);
    }
}
