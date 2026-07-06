using System.Net;

namespace Soundtrail.Services.Tests.Integration.GetArtist;

public sealed class ArtistExistsTests
{
    [Fact]
    public async Task Given_An_Existing_Artist_When_Requesting_The_Artist_Then_Ok_Is_Returned()
    {
        using var environment = GetArtistIntegrationTestEnvironment.ForExistingArtist();

        var response = await environment.GetAsync();

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Given_An_Existing_Artist_When_Requesting_The_Artist_Then_The_Artist_Id_Is_Returned()
    {
        using var environment = GetArtistIntegrationTestEnvironment.ForExistingArtist(artistId: "artist-702");

        var payload = await environment.GetPayloadAsync();

        payload.ArtistId.Should().Be("artist-702");
    }

    [Fact]
    public async Task Given_An_Existing_Artist_When_Requesting_The_Artist_Then_The_Artist_Name_Is_Returned()
    {
        using var environment = GetArtistIntegrationTestEnvironment.ForExistingArtist(artistName: "Artist 703");

        var payload = await environment.GetPayloadAsync();

        payload.ArtistName.Should().Be("Artist 703");
    }

    [Fact]
    public async Task Given_An_Existing_Artist_When_Requesting_The_Artist_Then_The_Description_Is_Returned()
    {
        using var environment = GetArtistIntegrationTestEnvironment.ForExistingArtist(description: "Artist 704 Description");

        var payload = await environment.GetPayloadAsync();

        payload.Description.Should().Be("Artist 704 Description");
    }

    [Fact]
    public async Task Given_An_Existing_Artist_When_Requesting_The_Artist_Then_The_Image_Url_Is_Returned()
    {
        using var environment = GetArtistIntegrationTestEnvironment.ForExistingArtist(imageUrl: "https://cdn.soundtrail.test/artists/artist-705.jpg");

        var payload = await environment.GetPayloadAsync();

        payload.ImageUrl.Should().Be("https://cdn.soundtrail.test/artists/artist-705.jpg");
    }
}
