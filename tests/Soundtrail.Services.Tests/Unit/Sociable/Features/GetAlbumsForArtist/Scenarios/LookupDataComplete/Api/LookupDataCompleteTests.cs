using Soundtrail.Domain.Catalog;
using Soundtrail.Domain.Catalog.Albums;
using Soundtrail.Domain.Common;
using Soundtrail.Services.Api.Features.Catalog.GetAlbumsForArtist.Contract;
using Soundtrail.Services.Tests.Unit.Sociable.Features.GetAlbumsForArtist.Support;
using Soundtrail.Services.Tests.Unit.Sociable.Features.GetAlbumsForArtist;

namespace Soundtrail.Services.Tests.Unit.Sociable.Features.GetAlbumsForArtist.Scenarios.LookupDataComplete.Api;

public sealed class LookupDataCompleteTests
{
    [Fact]
    public async Task Given_Lookup_Has_Completed_When_Requesting_Then_Artist_Id_Is_Returned()
    {
        var environment = await GetAlbumsForArtistSociableTestEnvironment.ForExistingCompletedLookup(MidnightSignals());

        var response = await environment.ProjectOnChange(
            sut => sut.Handle(new GetAlbumsForArtistRequest(environment.ArtistId)));

        response!.ArtistId.Should().Be(environment.ArtistId);
    }

    [Fact]
    public async Task Given_Lookup_Has_Completed_When_Requesting_Then_Albums_Are_Returned()
    {
        var albums = new[] { MidnightSignals(), StaticHearts() };
        var environment = await GetAlbumsForArtistSociableTestEnvironment.ForExistingCompletedLookup(albums);

        var response = await environment.ProjectOnChange(
            sut => sut.Handle(new GetAlbumsForArtistRequest(environment.ArtistId)));

        response!.Albums.Should().HaveCount(albums.Length);
    }

    [Fact]
    public async Task Given_Lookup_Has_Completed_When_Requesting_Then_Album_Title_Is_Returned()
    {
        var environment = await GetAlbumsForArtistSociableTestEnvironment.ForExistingCompletedLookup(MidnightSignals());

        var response = await environment.ProjectOnChange(
            sut => sut.Handle(new GetAlbumsForArtistRequest(environment.ArtistId)));

        response!.Albums.Single(album => album.AlbumTitle == "Midnight Signals").AlbumTitle.Should().Be("Midnight Signals");
    }

    [Fact]
    public async Task Given_Lookup_Has_Completed_When_Requesting_Then_Album_Id_Is_Returned()
    {
        var environment = await GetAlbumsForArtistSociableTestEnvironment.ForExistingCompletedLookup(MidnightSignals());
        var expected = AlbumId.From(environment.ArtistId.Value, "mb-release-midnight-signals");

        var response = await environment.ProjectOnChange(
            sut => sut.Handle(new GetAlbumsForArtistRequest(environment.ArtistId)));

        response!.Albums.Single(album => album.AlbumTitle == "Midnight Signals").AlbumId.Should().Be(expected);
    }

    [Fact]
    public async Task Given_Lookup_Has_Completed_When_Requesting_Then_Music_Catalog_Id_Is_Returned()
    {
        var environment = await GetAlbumsForArtistSociableTestEnvironment.ForExistingCompletedLookup(MidnightSignals());
        var expected = new CatalogItemId.Album(AlbumId.From(environment.ArtistId.Value, "mb-release-midnight-signals"));

        var response = await environment.ProjectOnChange(
            sut => sut.Handle(new GetAlbumsForArtistRequest(environment.ArtistId)));

        response!.Albums.Single(album => album.AlbumTitle == "Midnight Signals").MusicCatalogId.Should().Be(expected);
    }

    [Fact]
    public async Task Given_Lookup_Has_Completed_When_Requesting_Then_Release_Date_Is_Returned()
    {
        var releaseDate = new DateOnly(2023, 11, 10);
        var environment = await GetAlbumsForArtistSociableTestEnvironment.ForExistingCompletedLookup(MidnightSignals());

        var response = await environment.ProjectOnChange(
            sut => sut.Handle(new GetAlbumsForArtistRequest(environment.ArtistId)));

        response!.Albums.Single(album => album.AlbumTitle == "Midnight Signals").ReleaseDate.Should().Be(releaseDate);
    }

    [Fact]
    public async Task Given_Lookup_Has_Completed_When_Requesting_Then_Artwork_Url_Is_Returned()
    {
        var artworkUrl = "https://cdn.soundtrail.test/albums/midnight-signals.jpg";
        var environment = await GetAlbumsForArtistSociableTestEnvironment.ForExistingCompletedLookup(MidnightSignals());

        var response = await environment.ProjectOnChange(
            sut => sut.Handle(new GetAlbumsForArtistRequest(environment.ArtistId)));

        response!.Albums.Single(album => album.AlbumTitle == "Midnight Signals").ArtworkUrl.Should().Be(artworkUrl);
    }

    [Fact]
    public async Task Given_Lookup_Has_Completed_When_Requesting_Then_Discovery_Is_Completed()
    {
        var environment = await GetAlbumsForArtistSociableTestEnvironment.ForExistingCompletedLookup(MidnightSignals());

        var response = await environment.ProjectOnChange(
            sut => sut.Handle(new GetAlbumsForArtistRequest(environment.ArtistId)));

        response!.Discovery!.Status.Should().Be("completed");
    }

    [Fact]
    public async Task Given_Lookup_Has_Completed_When_Requesting_Then_Discovery_Has_High_Priority()
    {
        var environment = await GetAlbumsForArtistSociableTestEnvironment.ForExistingCompletedLookup(MidnightSignals());

        var response = await environment.ProjectOnChange(
            sut => sut.Handle(new GetAlbumsForArtistRequest(environment.ArtistId)));

        response!.Discovery!.Priority.Should().Be(LookupPriorityBand.High);
    }

    private static LookupDataCompleteArtistAlbum MidnightSignals() =>
        LookupDataCompleteArtistAlbumScenarios.MidnightSignals(default);

    private static LookupDataCompleteArtistAlbum StaticHearts() =>
        LookupDataCompleteArtistAlbumScenarios.StaticHearts(default);
}
