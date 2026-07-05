using System.Net;

namespace Soundtrail.Services.Tests.Integration.GetAlbum;

public sealed class AlbumExistsTests
{
    [Fact]
    public async Task Given_An_Existing_Album_When_Requesting_The_Album_Then_Ok_Is_Returned()
    {
        using var environment = GetAlbumIntegrationTestEnvironment.ForExistingAlbum();

        var response = await environment.GetAsync();

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Given_An_Existing_Album_When_Requesting_The_Album_Then_The_Artist_Id_Is_Returned()
    {
        using var environment = GetAlbumIntegrationTestEnvironment.ForExistingAlbum(artistId: "artist-302");

        var payload = await environment.GetPayloadAsync();

        payload.ArtistId.Should().Be("artist-302");
    }

    [Fact]
    public async Task Given_An_Existing_Album_When_Requesting_The_Album_Then_The_Artist_Name_Is_Returned()
    {
        using var environment = GetAlbumIntegrationTestEnvironment.ForExistingAlbum(artistName: "Artist 303");

        var payload = await environment.GetPayloadAsync();

        payload.ArtistName.Should().Be("Artist 303");
    }

    [Fact]
    public async Task Given_An_Existing_Album_When_Requesting_The_Album_Then_The_Album_Id_Is_Returned()
    {
        using var environment = GetAlbumIntegrationTestEnvironment.ForExistingAlbum(artistId: "artist-304", albumId: "album-504");

        var payload = await environment.GetPayloadAsync();

        payload.AlbumId.Should().Be("album-504");
    }

    [Fact]
    public async Task Given_An_Existing_Album_When_Requesting_The_Album_Then_The_Release_Date_Is_Returned()
    {
        var releaseDate = new DateOnly(2024, 11, 12);
        using var environment = GetAlbumIntegrationTestEnvironment.ForExistingAlbum(releaseDate: releaseDate);

        var payload = await environment.GetPayloadAsync();

        payload.ReleaseDate.Should().Be(releaseDate);
    }
}
