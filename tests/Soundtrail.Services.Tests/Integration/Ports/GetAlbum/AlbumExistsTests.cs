using Soundtrail.Domain.Catalog;

namespace Soundtrail.Services.Tests.Integration.Ports.GetAlbum;

public sealed class AlbumExistsTests
{
    public static TheoryData<GetAlbumPortImplementation> Implementations => new()
    {
        GetAlbumPortImplementation.Fake,
        GetAlbumPortImplementation.Raven
    };

    [Theory]
    [MemberData(nameof(Implementations))]
    public async Task Given_An_Existing_Album_When_Requesting_The_Album_Then_An_Album_Is_Returned(GetAlbumPortImplementation implementation)
    {
        await using var environment = await GetAlbumPortContractTestEnvironment.ForExistingAlbum(implementation);

        var result = await environment.Subject.GetAlbumAsync(environment.AlbumId, CancellationToken.None);

        result.Should().NotBeNull();
    }

    [Theory]
    [MemberData(nameof(Implementations))]
    public async Task Given_An_Existing_Album_When_Requesting_The_Album_Then_The_Artist_Id_Is_Returned(GetAlbumPortImplementation implementation)
    {
        await using var environment = await GetAlbumPortContractTestEnvironment.ForExistingAlbum(implementation, artistId: "artist-903");

        var result = await environment.Subject.GetAlbumAsync(environment.AlbumId, CancellationToken.None);

        result!.ArtistId.Value.Should().Be("artist-903");
    }

    [Theory]
    [MemberData(nameof(Implementations))]
    public async Task Given_An_Existing_Album_When_Requesting_The_Album_Then_The_Artist_Name_Is_Returned(GetAlbumPortImplementation implementation)
    {
        await using var environment = await GetAlbumPortContractTestEnvironment.ForExistingAlbum(implementation, artistName: "Artist 904");

        var result = await environment.Subject.GetAlbumAsync(environment.AlbumId, CancellationToken.None);

        result!.ArtistName.Value.Should().Be("Artist 904");
    }

    [Theory]
    [MemberData(nameof(Implementations))]
    public async Task Given_An_Existing_Album_When_Requesting_The_Album_Then_The_Album_Id_Is_Returned(GetAlbumPortImplementation implementation)
    {
        await using var environment = await GetAlbumPortContractTestEnvironment.ForExistingAlbum(implementation, artistId: "artist-905", albumId: "album-905");

        var result = await environment.Subject.GetAlbumAsync(environment.AlbumId, CancellationToken.None);

        result!.AlbumId.Should().Be(AlbumId.From("artist-905", "album-905"));
    }

    [Theory]
    [MemberData(nameof(Implementations))]
    public async Task Given_An_Existing_Album_When_Requesting_The_Album_Then_The_Album_Name_Is_Returned(GetAlbumPortImplementation implementation)
    {
        await using var environment = await GetAlbumPortContractTestEnvironment.ForExistingAlbum(implementation, albumName: "Album 906");

        var result = await environment.Subject.GetAlbumAsync(environment.AlbumId, CancellationToken.None);

        result!.AlbumName.Should().Be("Album 906");
    }

    [Theory]
    [MemberData(nameof(Implementations))]
    public async Task Given_An_Existing_Album_When_Requesting_The_Album_Then_The_Release_Date_Is_Returned(GetAlbumPortImplementation implementation)
    {
        var releaseDate = new DateOnly(2024, 11, 12);
        await using var environment = await GetAlbumPortContractTestEnvironment.ForExistingAlbum(implementation, releaseDate: releaseDate);

        var result = await environment.Subject.GetAlbumAsync(environment.AlbumId, CancellationToken.None);

        result!.ReleaseDate.Should().Be(releaseDate);
    }
}
