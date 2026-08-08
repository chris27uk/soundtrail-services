using Soundtrail.Domain.Catalog.Albums;
using Soundtrail.Services.Tests.Integration.Features.GetAlbum;

namespace Soundtrail.Services.Tests.Integration.Features.GetAlbum.Scenarios.Api.Ports;

public sealed class AlbumExistsTests
{
    public static TheoryData<GetAlbumPortImplementation> Implementations => new()
    {
        GetAlbumPortImplementation.Raven
    };

    [Theory]
    [MemberData(nameof(Implementations))]
    public async Task Given_An_Existing_Album_When_Requesting_The_Album_Then_An_Album_Is_Returned(
        GetAlbumPortImplementation implementation)
    {
        var artistId = "artist-901";
        var albumId = "album-901";
        await using var environment = await GetAlbumPortContractTestEnvironment.ForExistingAlbum(
            implementation,
            artistId: artistId,
            albumId: albumId);

        var result = await environment.Subject.GetAlbumAsync(environment.AlbumId, CancellationToken.None);

        result.Should().NotBeNull();
    }

    [Theory]
    [MemberData(nameof(Implementations))]
    public async Task Given_An_Existing_Album_When_Requesting_The_Album_Then_The_Artist_Id_Is_Returned(
        GetAlbumPortImplementation implementation)
    {
        var artistId = "artist-903";
        await using var environment = await GetAlbumPortContractTestEnvironment.ForExistingAlbum(
            implementation,
            artistId: artistId);

        var result = await environment.Subject.GetAlbumAsync(environment.AlbumId, CancellationToken.None);

        result!.ArtistId.Value.Should().Be(artistId);
    }

    [Theory]
    [MemberData(nameof(Implementations))]
    public async Task Given_An_Existing_Album_When_Requesting_The_Album_Then_The_Artist_Name_Is_Returned(
        GetAlbumPortImplementation implementation)
    {
        var artistName = "Artist 904";
        await using var environment = await GetAlbumPortContractTestEnvironment.ForExistingAlbum(
            implementation,
            artistName: artistName);

        var result = await environment.Subject.GetAlbumAsync(environment.AlbumId, CancellationToken.None);

        result!.ArtistName.Value.Should().Be(artistName);
    }

    [Theory]
    [MemberData(nameof(Implementations))]
    public async Task Given_An_Existing_Album_When_Requesting_The_Album_Then_The_Album_Id_Is_Returned(
        GetAlbumPortImplementation implementation)
    {
        var artistId = "artist-905";
        var albumId = "album-905";
        var expectedAlbumId = AlbumId.From(artistId, albumId);
        await using var environment = await GetAlbumPortContractTestEnvironment.ForExistingAlbum(
            implementation,
            artistId: artistId,
            albumId: albumId);

        var result = await environment.Subject.GetAlbumAsync(environment.AlbumId, CancellationToken.None);

        result!.AlbumId.Should().Be(expectedAlbumId);
    }

    [Theory]
    [MemberData(nameof(Implementations))]
    public async Task Given_An_Existing_Album_When_Requesting_The_Album_Then_The_Album_Name_Is_Returned(
        GetAlbumPortImplementation implementation)
    {
        var albumName = "Album 906";
        await using var environment = await GetAlbumPortContractTestEnvironment.ForExistingAlbum(
            implementation,
            albumName: albumName);

        var result = await environment.Subject.GetAlbumAsync(environment.AlbumId, CancellationToken.None);

        result!.AlbumName.Should().Be(albumName);
    }

    [Theory]
    [MemberData(nameof(Implementations))]
    public async Task Given_An_Existing_Album_When_Requesting_The_Album_Then_The_Release_Date_Is_Returned(
        GetAlbumPortImplementation implementation)
    {
        var releaseDate = new DateOnly(2024, 11, 12);
        await using var environment = await GetAlbumPortContractTestEnvironment.ForExistingAlbum(
            implementation,
            releaseDate: releaseDate);

        var result = await environment.Subject.GetAlbumAsync(environment.AlbumId, CancellationToken.None);

        result!.ReleaseDate.Should().Be(releaseDate);
    }
}
