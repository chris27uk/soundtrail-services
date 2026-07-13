using Soundtrail.Domain.Catalog;

namespace Soundtrail.Services.Tests.Integration.Ports.GetAlbumsForArtist;

public sealed class ArtistAlbumsExistTests
{
    public static TheoryData<GetAlbumsForArtistPortImplementation> Implementations => new()
    {
        GetAlbumsForArtistPortImplementation.Fake,
        GetAlbumsForArtistPortImplementation.Raven
    };

    [Theory]
    [MemberData(nameof(Implementations))]
    public async Task Given_Existing_Artist_Albums_When_Requesting_The_Artist_Albums_Then_Artist_Albums_Are_Returned(GetAlbumsForArtistPortImplementation implementation)
    {
        await using var environment = await GetAlbumsForArtistPortContractTestEnvironment.ForExistingArtistAlbums(implementation);

        var result = await environment.Subject.GetAlbumsForArtistAsync(environment.ArtistId, CancellationToken.None);

        result.Should().NotBeNull();
    }

    [Theory]
    [MemberData(nameof(Implementations))]
    public async Task Given_Existing_Artist_Albums_When_Requesting_The_Artist_Albums_Then_The_Artist_Id_Is_Returned(GetAlbumsForArtistPortImplementation implementation)
    {
        await using var environment = await GetAlbumsForArtistPortContractTestEnvironment.ForExistingArtistAlbums(implementation, artistId: "artist-2103");

        var result = await environment.Subject.GetAlbumsForArtistAsync(environment.ArtistId, CancellationToken.None);

        result!.ArtistId.Should().Be(ArtistId.From("artist-2103"));
    }

    [Theory]
    [MemberData(nameof(Implementations))]
    public async Task Given_Existing_Artist_Albums_When_Requesting_The_Artist_Albums_Then_The_Artist_Name_Is_Returned(GetAlbumsForArtistPortImplementation implementation)
    {
        await using var environment = await GetAlbumsForArtistPortContractTestEnvironment.ForExistingArtistAlbums(implementation, artistName: "Artist 2104");

        var result = await environment.Subject.GetAlbumsForArtistAsync(environment.ArtistId, CancellationToken.None);

        result!.ArtistName.Value.Should().Be("Artist 2104");
    }

    [Theory]
    [MemberData(nameof(Implementations))]
    public async Task Given_Existing_Artist_Albums_When_Requesting_The_Artist_Albums_Then_The_Albums_Are_Returned(GetAlbumsForArtistPortImplementation implementation)
    {
        await using var environment = await GetAlbumsForArtistPortContractTestEnvironment.ForExistingArtistAlbums(implementation);

        var result = await environment.Subject.GetAlbumsForArtistAsync(environment.ArtistId, CancellationToken.None);

        result!.Albums.Should().HaveCount(1);
    }

    [Theory]
    [MemberData(nameof(Implementations))]
    public async Task Given_Existing_Artist_Albums_When_Requesting_The_Artist_Albums_Then_The_Album_Id_Is_Returned(GetAlbumsForArtistPortImplementation implementation)
    {
        await using var environment = await GetAlbumsForArtistPortContractTestEnvironment.ForExistingArtistAlbums(implementation, artistId: "artist-2105", albumId: "album-2205");

        var result = await environment.Subject.GetAlbumsForArtistAsync(environment.ArtistId, CancellationToken.None);

        result!.Albums[0].AlbumId.Should().Be(AlbumId.From("artist-2105", "album-2205"));
    }

    [Theory]
    [MemberData(nameof(Implementations))]
    public async Task Given_Existing_Artist_Albums_When_Requesting_The_Artist_Albums_Then_The_Music_Catalog_Id_Is_Returned(GetAlbumsForArtistPortImplementation implementation)
    {
        await using var environment = await GetAlbumsForArtistPortContractTestEnvironment.ForExistingArtistAlbums(implementation, artistId: "artist-2106", albumId: "album-2206");

        var result = await environment.Subject.GetAlbumsForArtistAsync(environment.ArtistId, CancellationToken.None);

        result!.Albums[0].MusicCatalogId.Should().Be(new MusicCatalogId.Album(AlbumId.From("artist-2106", "album-2206")));
    }

    [Theory]
    [MemberData(nameof(Implementations))]
    public async Task Given_Existing_Artist_Albums_When_Requesting_The_Artist_Albums_Then_The_Album_Title_Is_Returned(GetAlbumsForArtistPortImplementation implementation)
    {
        await using var environment = await GetAlbumsForArtistPortContractTestEnvironment.ForExistingArtistAlbums(implementation, albumTitle: "Album 2207");

        var result = await environment.Subject.GetAlbumsForArtistAsync(environment.ArtistId, CancellationToken.None);

        result!.Albums[0].AlbumTitle.Should().Be("Album 2207");
    }

    [Theory]
    [MemberData(nameof(Implementations))]
    public async Task Given_Existing_Artist_Albums_When_Requesting_The_Artist_Albums_Then_The_Release_Date_Is_Returned(GetAlbumsForArtistPortImplementation implementation)
    {
        var releaseDate = new DateOnly(2024, 11, 12);
        await using var environment = await GetAlbumsForArtistPortContractTestEnvironment.ForExistingArtistAlbums(implementation, releaseDate: releaseDate);

        var result = await environment.Subject.GetAlbumsForArtistAsync(environment.ArtistId, CancellationToken.None);

        result!.Albums[0].ReleaseDate.Should().Be(releaseDate);
    }

    [Theory]
    [MemberData(nameof(Implementations))]
    public async Task Given_Existing_Artist_Albums_When_Requesting_The_Artist_Albums_Then_The_Artwork_Url_Is_Returned(GetAlbumsForArtistPortImplementation implementation)
    {
        await using var environment = await GetAlbumsForArtistPortContractTestEnvironment.ForExistingArtistAlbums(implementation, artworkUrl: "https://cdn.soundtrail.test/albums/album-2209.jpg");

        var result = await environment.Subject.GetAlbumsForArtistAsync(environment.ArtistId, CancellationToken.None);

        result!.Albums[0].ArtworkUrl.Should().Be("https://cdn.soundtrail.test/albums/album-2209.jpg");
    }
}
