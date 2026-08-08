using Soundtrail.Domain.Catalog.Albums;

namespace Soundtrail.Services.Tests.Unit.Sociable.Features.GetAlbum.Scenarios.DataAvailable.Api;

public sealed class DataAvailableTests
{
    [Fact]
    public async Task When_Requesting_Then_An_Album_Is_Returned()
    {
        var albumId = AlbumId.From("artist-101", "album-201");
        var environment = GetAlbumSociableTestEnvironment.ForDataAvailable(albumId: albumId);

        var result = await environment.ProjectOnChange(
            sut => sut.Handle(environment.CreateRequest()));

        result.Should().NotBeNull();
    }

    [Fact]
    public async Task When_Requesting_Then_The_Artist_Id_Is_Returned()
    {
        var artistId = "artist-103";
        var albumId = AlbumId.From(artistId, "album-203");
        var environment = GetAlbumSociableTestEnvironment.ForDataAvailable(albumId: albumId);

        var result = await environment.ProjectOnChange(
            sut => sut.Handle(environment.CreateRequest()));

        result!.ArtistId.Value.Should().Be(artistId);
    }

    [Fact]
    public async Task When_Requesting_Then_The_Artist_Name_Is_Returned()
    {
        var artistName = "Artist 104";
        var environment = GetAlbumSociableTestEnvironment.ForDataAvailable(
            response: GetAlbumScenarioData.CreateResponse(artistName: artistName));

        var result = await environment.ProjectOnChange(
            sut => sut.Handle(environment.CreateRequest()));

        result!.ArtistName.Value.Should().Be(artistName);
    }

    [Fact]
    public async Task When_Requesting_Then_The_Album_Id_Is_Returned()
    {
        var albumId = AlbumId.From("artist-105", "album-205");
        var environment = GetAlbumSociableTestEnvironment.ForDataAvailable(albumId: albumId);

        var result = await environment.ProjectOnChange(
            sut => sut.Handle(environment.CreateRequest()));

        result!.AlbumId.Should().Be(albumId);
    }

    [Fact]
    public async Task When_Requesting_Then_The_Album_Name_Is_Returned()
    {
        var albumName = "Album 106";
        var environment = GetAlbumSociableTestEnvironment.ForDataAvailable(
            response: GetAlbumScenarioData.CreateResponse(albumName: albumName));

        var result = await environment.ProjectOnChange(
            sut => sut.Handle(environment.CreateRequest()));

        result!.AlbumName.Should().Be(albumName);
    }

    [Fact]
    public async Task When_Requesting_Then_The_Release_Date_Is_Returned()
    {
        var releaseDate = new DateOnly(2024, 11, 12);
        var environment = GetAlbumSociableTestEnvironment.ForDataAvailable(
            response: GetAlbumScenarioData.CreateResponse(releaseDate: releaseDate));

        var result = await environment.ProjectOnChange(
            sut => sut.Handle(environment.CreateRequest()));

        result!.ReleaseDate.Should().Be(releaseDate);
    }

    [Fact]
    public async Task When_Requesting_Then_The_Requested_Album_Id_Is_Read()
    {
        var albumId = AlbumId.From("artist-102", "album-202");
        var environment = GetAlbumSociableTestEnvironment.ForDataAvailable(albumId: albumId);

        await environment.ProjectOnChange(sut => sut.Handle(environment.CreateRequest()));

        environment.Port.RequestedAlbumIds.Single().Should().Be(albumId);
    }

    [Fact]
    public async Task When_Requesting_Then_No_Enrichment_Work_Is_Scheduled()
    {
        var albumId = AlbumId.From("artist-101", "album-201");
        var environment = GetAlbumSociableTestEnvironment.ForDataAvailable(albumId: albumId);

        await environment.ProjectOnChange(sut => sut.Handle(environment.CreateRequest()));

        environment.SentMessages.Should().BeEmpty();
    }
}
