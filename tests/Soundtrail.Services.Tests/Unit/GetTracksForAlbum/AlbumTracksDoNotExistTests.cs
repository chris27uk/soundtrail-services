using Soundtrail.Domain.Catalog;

namespace Soundtrail.Services.Tests.Unit.GetTracksForAlbum;

public sealed class AlbumTracksDoNotExistTests
{
    [Fact]
    public async Task Given_Missing_Album_Tracks_When_Requesting_The_Album_Tracks_Then_No_Album_Tracks_Are_Returned()
    {
        var environment = GetTracksForAlbumMissingUnitTestEnvironment.ForMissingAlbumTracks();

        var result = await environment.CreateSubjectUnderTest().Handle(environment.CreateRequest());

        result.Should().BeNull();
    }

    [Fact]
    public async Task Given_Missing_Album_Tracks_When_Requesting_The_Album_Tracks_Then_The_Requested_Album_Id_Is_Read()
    {
        var albumId = AlbumId.From("artist-1407", "album-1507");
        var environment = GetTracksForAlbumMissingUnitTestEnvironment.ForMissingAlbumTracks(albumId);

        await environment.CreateSubjectUnderTest().Handle(environment.CreateRequest());

        environment.Port.RequestedAlbumIds.Single().Should().Be(albumId);
    }
}
