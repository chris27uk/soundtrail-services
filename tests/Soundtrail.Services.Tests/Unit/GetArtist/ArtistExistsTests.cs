using Soundtrail.Domain.Catalog;
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
