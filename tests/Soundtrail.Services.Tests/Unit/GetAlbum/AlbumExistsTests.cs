using Soundtrail.Domain.Catalog;
using Soundtrail.Services.Api.Features.GetAlbum.Contract;

namespace Soundtrail.Services.Tests.Unit.GetAlbum;

public sealed class AlbumExistsTests
{
    [Fact]
    public async Task Given_An_Existing_Album_When_Requesting_The_Album_Then_The_Port_Response_Is_Returned()
    {
        var response = AlbumExistsTestData.CreateResponse();
        var environment = GetAlbumUnitTestEnvironment.ForExistingAlbum(response: response);

        var result = await environment.CreateSubjectUnderTest().Handle(environment.CreateRequest());

        result.Should().BeSameAs(response);
    }

    [Fact]
    public async Task Given_An_Existing_Album_When_Requesting_The_Album_Then_The_Requested_Album_Id_Is_Read()
    {
        var albumId = AlbumId.From("artist-102", "album-202");
        var environment = GetAlbumUnitTestEnvironment.ForExistingAlbum(albumId: albumId);

        await environment.CreateSubjectUnderTest().Handle(environment.CreateRequest());

        environment.Port.RequestedAlbumIds.Single().Should().Be(albumId);
    }
}

internal static class AlbumExistsTestData
{
    public static AlbumId DefaultAlbumId => AlbumId.From("artist-101", "album-201");

    public static GetAlbumResponse CreateResponse(
        AlbumId? albumId = null,
        string artistName = "The Artist",
        DateOnly? releaseDate = null)
    {
        var resolvedAlbumId = albumId ?? DefaultAlbumId;
        return new GetAlbumResponse(
            ArtistId.From(resolvedAlbumId.ArtistId),
            ArtistName.From(artistName),
            resolvedAlbumId,
            "The Album",
            releaseDate ?? new DateOnly(2024, 1, 2));
    }
}
