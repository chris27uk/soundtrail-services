using Soundtrail.Contracts.Common;
using Soundtrail.Domain.Catalog;
using Soundtrail.Domain.Catalog.Albums;
using Soundtrail.Domain.Catalog.Artists;
using Soundtrail.Domain.Common;
using Soundtrail.Domain.Discovery;
using Soundtrail.Domain.Discovery.Events;

namespace Soundtrail.Services.Tests.Unit.GetAlbumsForArtist;

public sealed class ArtistAlbumsExistTests
{
    [Fact]
    public async Task Given_Existing_Artist_Albums_When_Requesting_The_Artist_Albums_Then_The_Port_Response_Is_Returned()
    {
        var response = ArtistAlbums.CreateResponse();
        var environment = GetAlbumsForArtistUnitTestEnvironment.ForExistingArtistAlbums(response: response);

        var result = await environment.CreateSubjectUnderTest().Handle(environment.CreateRequest());

        result.Should().BeSameAs(response);
    }

    [Fact]
    public async Task Given_Existing_Artist_Albums_When_Requesting_The_Artist_Albums_Then_The_Artist_Id_Is_Returned()
    {
        var artistId = ArtistId.From("artist-1703");
        var environment = GetAlbumsForArtistUnitTestEnvironment.ForExistingArtistAlbums(artistId: artistId);

        var result = await environment.CreateSubjectUnderTest().Handle(environment.CreateRequest());

        result!.ArtistId.Should().Be(artistId);
    }

    [Fact]
    public async Task Given_Existing_Artist_Albums_When_Requesting_The_Artist_Albums_Then_The_Artist_Name_Is_Returned()
    {
        var environment = GetAlbumsForArtistUnitTestEnvironment.ForExistingArtistAlbums(
            response: ArtistAlbums.CreateResponse(artistName: "Artist 1704"));

        var result = await environment.CreateSubjectUnderTest().Handle(environment.CreateRequest());

        result!.ArtistName.Value.Should().Be("Artist 1704");
    }

    [Fact]
    public async Task Given_Existing_Artist_Albums_When_Requesting_The_Artist_Albums_Then_The_Albums_Are_Returned()
    {
        var environment = GetAlbumsForArtistUnitTestEnvironment.ForExistingArtistAlbums();

        var result = await environment.CreateSubjectUnderTest().Handle(environment.CreateRequest());

        result!.Albums.Should().HaveCount(1);
    }

    [Fact]
    public async Task Given_Existing_Artist_Albums_When_Requesting_The_Artist_Albums_Then_The_Album_Id_Is_Returned()
    {
        var artistId = ArtistId.From("artist-1705");
        var response = ArtistAlbums.CreateResponse(artistId: artistId, albumId: "album-1805");
        var environment = GetAlbumsForArtistUnitTestEnvironment.ForExistingArtistAlbums(artistId: artistId, response: response);

        var result = await environment.CreateSubjectUnderTest().Handle(environment.CreateRequest());

        result!.Albums[0].AlbumId.Should().Be(AlbumId.From("artist-1705", "album-1805"));
    }

    [Fact]
    public async Task Given_Existing_Artist_Albums_When_Requesting_The_Artist_Albums_Then_The_Music_Catalog_Id_Is_Returned()
    {
        var artistId = ArtistId.From("artist-1706");
        var response = ArtistAlbums.CreateResponse(artistId: artistId, albumId: "album-1806");
        var environment = GetAlbumsForArtistUnitTestEnvironment.ForExistingArtistAlbums(artistId: artistId, response: response);

        var result = await environment.CreateSubjectUnderTest().Handle(environment.CreateRequest());

        result!.Albums[0].MusicCatalogId.Should().Be(new CatalogItemId.Album(AlbumId.From("artist-1706", "album-1806")));
    }

    [Fact]
    public async Task Given_Existing_Artist_Albums_When_Requesting_The_Artist_Albums_Then_The_Album_Title_Is_Returned()
    {
        var environment = GetAlbumsForArtistUnitTestEnvironment.ForExistingArtistAlbums(
            response: ArtistAlbums.CreateResponse(albumTitle: "Album 1807"));

        var result = await environment.CreateSubjectUnderTest().Handle(environment.CreateRequest());

        result!.Albums[0].AlbumTitle.Should().Be("Album 1807");
    }

    [Fact]
    public async Task Given_Existing_Artist_Albums_When_Requesting_The_Artist_Albums_Then_The_Release_Date_Is_Returned()
    {
        var releaseDate = new DateOnly(2024, 11, 12);
        var environment = GetAlbumsForArtistUnitTestEnvironment.ForExistingArtistAlbums(
            response: ArtistAlbums.CreateResponse(releaseDate: releaseDate));

        var result = await environment.CreateSubjectUnderTest().Handle(environment.CreateRequest());

        result!.Albums[0].ReleaseDate.Should().Be(releaseDate);
    }

    [Fact]
    public async Task Given_Existing_Artist_Albums_When_Requesting_The_Artist_Albums_Then_The_Artwork_Url_Is_Returned()
    {
        var environment = GetAlbumsForArtistUnitTestEnvironment.ForExistingArtistAlbums(
            response: ArtistAlbums.CreateResponse(artworkUrl: "https://cdn.soundtrail.test/albums/album-1809.jpg"));

        var result = await environment.CreateSubjectUnderTest().Handle(environment.CreateRequest());

        result!.Albums[0].ArtworkUrl.Should().Be("https://cdn.soundtrail.test/albums/album-1809.jpg");
    }

    [Fact]
    public async Task Given_Existing_Artist_Albums_When_Requesting_The_Artist_Albums_Then_The_Requested_Artist_Id_Is_Read()
    {
        var artistId = ArtistId.From("artist-1702");
        var environment = GetAlbumsForArtistUnitTestEnvironment.ForExistingArtistAlbums(artistId: artistId);

        await environment.CreateSubjectUnderTest().Handle(environment.CreateRequest());

        environment.Port.RequestedArtistIds.Single().Should().Be(artistId);
    }

    [Fact]
    public async Task Given_Existing_Artist_Albums_When_Requesting_The_Artist_Albums_Then_A_Search_Command_Is_Sent()
    {
        var environment = GetAlbumsForArtistUnitTestEnvironment.ForExistingArtistAlbums();

        await environment.CreateSubjectUnderTest().Handle(environment.CreateRequest());

        environment.CommandBus.Commands.Should().ContainSingle();
    }

    [Fact]
    public async Task Given_Existing_Artist_Albums_When_Requesting_The_Artist_Albums_Then_The_Search_Command_Filter_Is_Artist_Based()
    {
        var artistId = ArtistId.From("artist-1708");
        var environment = GetAlbumsForArtistUnitTestEnvironment.ForExistingArtistAlbums(artistId: artistId);

        await environment.CreateSubjectUnderTest().Handle(environment.CreateRequest());

        environment.CommandBus.Commands.Single().Operation.Should().Be(new CatalogItemOperation.ChildAlbumsForArtist(artistId));
    }

    [Fact]
    public async Task Given_Existing_Artist_Albums_When_Requesting_The_Artist_Albums_Then_The_Search_Command_Has_High_Priority()
    {
        var environment = GetAlbumsForArtistUnitTestEnvironment.ForExistingArtistAlbums();

        await environment.CreateSubjectUnderTest().Handle(environment.CreateRequest());

        environment.CommandBus.Commands.Single().Priority.Should().Be(LookupPriorityBand.High);
    }

    [Fact]
    public async Task Given_Existing_Artist_Albums_When_Requesting_The_Artist_Albums_Then_The_Search_Command_Trust_Level_Is_One_Hundred()
    {
        var environment = GetAlbumsForArtistUnitTestEnvironment.ForExistingArtistAlbums();

        await environment.CreateSubjectUnderTest().Handle(environment.CreateRequest());

        environment.CommandBus.Commands.Single().TrustLevel.Should().Be(100);
    }

    [Fact]
    public async Task Given_Existing_Artist_Albums_When_Requesting_The_Artist_Albums_Then_The_Search_Command_Risk_Score_Is_Zero()
    {
        var environment = GetAlbumsForArtistUnitTestEnvironment.ForExistingArtistAlbums();

        await environment.CreateSubjectUnderTest().Handle(environment.CreateRequest());

        environment.CommandBus.Commands.Single().RiskScore.Should().Be(0);
    }

    [Fact]
    public async Task Given_Existing_Artist_Albums_When_Requesting_The_Artist_Albums_Then_The_Search_Command_Requested_At_Is_Set()
    {
        var environment = GetAlbumsForArtistUnitTestEnvironment.ForExistingArtistAlbums();

        await environment.CreateSubjectUnderTest().Handle(environment.CreateRequest());

        environment.CommandBus.Commands.Single().RequestedAt.Should().Be(environment.Clock.UtcNow);
    }
}
