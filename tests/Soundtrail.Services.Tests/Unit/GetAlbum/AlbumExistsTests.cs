using Soundtrail.Domain.Catalog.Albums;
using Soundtrail.Domain.Catalog.Artists;
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
    public async Task Given_An_Existing_Album_When_Requesting_The_Album_Then_The_Artist_Id_Is_Returned()
    {
        var albumId = AlbumId.From("artist-103", "album-203");
        var environment = GetAlbumUnitTestEnvironment.ForExistingAlbum(albumId: albumId);

        var result = await environment.CreateSubjectUnderTest().Handle(environment.CreateRequest());

        result!.ArtistId.Value.Should().Be("artist-103");
    }

    [Fact]
    public async Task Given_An_Existing_Album_When_Requesting_The_Album_Then_The_Artist_Name_Is_Returned()
    {
        var environment = GetAlbumUnitTestEnvironment.ForExistingAlbum(
            response: AlbumExistsTestData.CreateResponse(artistName: "Artist 104"));

        var result = await environment.CreateSubjectUnderTest().Handle(environment.CreateRequest());

        result!.ArtistName.Value.Should().Be("Artist 104");
    }

    [Fact]
    public async Task Given_An_Existing_Album_When_Requesting_The_Album_Then_The_Album_Id_Is_Returned()
    {
        var albumId = AlbumId.From("artist-105", "album-205");
        var environment = GetAlbumUnitTestEnvironment.ForExistingAlbum(albumId: albumId);

        var result = await environment.CreateSubjectUnderTest().Handle(environment.CreateRequest());

        result!.AlbumId.Should().Be(albumId);
    }

    [Fact]
    public async Task Given_An_Existing_Album_When_Requesting_The_Album_Then_The_Album_Name_Is_Returned()
    {
        var environment = GetAlbumUnitTestEnvironment.ForExistingAlbum(
            response: AlbumExistsTestData.CreateResponse(albumName: "Album 106"));

        var result = await environment.CreateSubjectUnderTest().Handle(environment.CreateRequest());

        result!.AlbumName.Should().Be("Album 106");
    }

    [Fact]
    public async Task Given_An_Existing_Album_When_Requesting_The_Album_Then_The_Release_Date_Is_Returned()
    {
        var releaseDate = new DateOnly(2024, 11, 12);
        var environment = GetAlbumUnitTestEnvironment.ForExistingAlbum(
            response: AlbumExistsTestData.CreateResponse(releaseDate: releaseDate));

        var result = await environment.CreateSubjectUnderTest().Handle(environment.CreateRequest());

        result!.ReleaseDate.Should().Be(releaseDate);
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
        string albumName = "The Album",
        DateOnly? releaseDate = null)
    {
        var resolvedAlbumId = albumId ?? DefaultAlbumId;
        return new GetAlbumResponse(
            ArtistId.From(resolvedAlbumId.ArtistId),
            ArtistName.From(artistName),
            resolvedAlbumId,
            albumName,
            releaseDate ?? new DateOnly(2024, 1, 2));
    }
}
