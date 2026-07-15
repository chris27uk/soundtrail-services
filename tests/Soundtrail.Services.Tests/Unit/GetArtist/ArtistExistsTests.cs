using Soundtrail.Domain.Catalog;
using Soundtrail.Domain.Catalog.Artists;
using Soundtrail.Services.Api.Features.GetArtist.Contract;

namespace Soundtrail.Services.Tests.Unit.GetArtist;

public sealed class ArtistExistsTests
{
    [Fact]
    public async Task Given_An_Existing_Artist_When_Requesting_The_Artist_Then_The_Port_Response_Is_Returned()
    {
        var response = ArtistExistsTestData.CreateResponse();
        var environment = GetArtistUnitTestEnvironment.ForExistingArtist(response: response);

        var result = await environment.CreateSubjectUnderTest().Handle(environment.CreateRequest());

        result.Should().BeSameAs(response);
    }

    [Fact]
    public async Task Given_An_Existing_Artist_When_Requesting_The_Artist_Then_The_Artist_Id_Is_Returned()
    {
        var artistId = ArtistId.From("artist-503");
        var environment = GetArtistUnitTestEnvironment.ForExistingArtist(artistId: artistId);

        var result = await environment.CreateSubjectUnderTest().Handle(environment.CreateRequest());

        result!.ArtistId.Should().Be(artistId);
    }

    [Fact]
    public async Task Given_An_Existing_Artist_When_Requesting_The_Artist_Then_The_Artist_Name_Is_Returned()
    {
        var environment = GetArtistUnitTestEnvironment.ForExistingArtist(
            response: ArtistExistsTestData.CreateResponse(artistName: "Artist 504"));

        var result = await environment.CreateSubjectUnderTest().Handle(environment.CreateRequest());

        result!.ArtistName.Value.Should().Be("Artist 504");
    }

    [Fact]
    public async Task Given_An_Existing_Artist_When_Requesting_The_Artist_Then_The_Description_Is_Returned()
    {
        var environment = GetArtistUnitTestEnvironment.ForExistingArtist(
            response: ArtistExistsTestData.CreateResponse(description: "Artist 505 Description"));

        var result = await environment.CreateSubjectUnderTest().Handle(environment.CreateRequest());

        result!.Description.Should().Be("Artist 505 Description");
    }

    [Fact]
    public async Task Given_An_Existing_Artist_When_Requesting_The_Artist_Then_The_Image_Url_Is_Returned()
    {
        var environment = GetArtistUnitTestEnvironment.ForExistingArtist(
            response: ArtistExistsTestData.CreateResponse(imageUrl: "https://cdn.soundtrail.test/artists/artist-506.jpg"));

        var result = await environment.CreateSubjectUnderTest().Handle(environment.CreateRequest());

        result!.ImageUrl.Should().Be("https://cdn.soundtrail.test/artists/artist-506.jpg");
    }

    [Fact]
    public async Task Given_An_Existing_Artist_When_Requesting_The_Artist_Then_The_Requested_Artist_Id_Is_Read()
    {
        var artistId = ArtistId.From("artist-502");
        var environment = GetArtistUnitTestEnvironment.ForExistingArtist(artistId: artistId);

        await environment.CreateSubjectUnderTest().Handle(environment.CreateRequest());

        environment.Port.RequestedArtistIds.Single().Should().Be(artistId);
    }
}

internal static class ArtistExistsTestData
{
    public static ArtistId DefaultArtistId => ArtistId.From("artist-501");

    public static GetArtistResponse CreateResponse(
        ArtistId? artistId = null,
        string artistName = "The Artist",
        string? description = "An Artist Description",
        string? imageUrl = "https://cdn.soundtrail.test/artists/artist-501.jpg")
    {
        var resolvedArtistId = artistId ?? DefaultArtistId;
        return new GetArtistResponse(
            resolvedArtistId,
            ArtistName.From(artistName),
            description,
            imageUrl);
    }
}
