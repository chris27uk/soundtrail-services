using Soundtrail.Services.Tests.Integration.Features.GetArtist;
namespace Soundtrail.Services.Tests.Integration.Features.GetArtist.Scenarios.Api.Ports;

public sealed class ArtistExistsTests
{
    public static TheoryData<GetArtistPortImplementation> Implementations => new()
    {
        GetArtistPortImplementation.Raven
    };

    [Theory]
    [MemberData(nameof(Implementations))]
    public async Task Given_An_Existing_Artist_When_Requesting_The_Artist_Then_An_Artist_Is_Returned(
        GetArtistPortImplementation implementation)
    {
        var artistId = "artist-1001";
        await using var environment = await GetArtistPortContractTestEnvironment.ForExistingArtist(
            implementation,
            artistId: artistId);

        var result = await environment.Subject.GetArtistAsync(environment.ArtistId, CancellationToken.None);

        result.Should().NotBeNull();
    }

    [Theory]
    [MemberData(nameof(Implementations))]
    public async Task Given_An_Existing_Artist_When_Requesting_The_Artist_Then_The_Artist_Id_Is_Returned(
        GetArtistPortImplementation implementation)
    {
        var artistId = "artist-1003";
        await using var environment = await GetArtistPortContractTestEnvironment.ForExistingArtist(
            implementation,
            artistId: artistId);

        var result = await environment.Subject.GetArtistAsync(environment.ArtistId, CancellationToken.None);

        result!.ArtistId.Value.Should().Be(artistId);
    }

    [Theory]
    [MemberData(nameof(Implementations))]
    public async Task Given_An_Existing_Artist_When_Requesting_The_Artist_Then_The_Artist_Name_Is_Returned(
        GetArtistPortImplementation implementation)
    {
        var artistName = "Artist 1004";
        await using var environment = await GetArtistPortContractTestEnvironment.ForExistingArtist(
            implementation,
            artistName: artistName);

        var result = await environment.Subject.GetArtistAsync(environment.ArtistId, CancellationToken.None);

        result!.ArtistName.Value.Should().Be(artistName);
    }

    [Theory]
    [MemberData(nameof(Implementations))]
    public async Task Given_An_Existing_Artist_When_Requesting_The_Artist_Then_No_Description_Is_Returned(
        GetArtistPortImplementation implementation)
    {
        var artistId = "artist-1001";
        await using var environment = await GetArtistPortContractTestEnvironment.ForExistingArtist(
            implementation,
            artistId: artistId);

        var result = await environment.Subject.GetArtistAsync(environment.ArtistId, CancellationToken.None);

        result!.Description.Should().BeNull();
    }

    [Theory]
    [MemberData(nameof(Implementations))]
    public async Task Given_An_Existing_Artist_When_Requesting_The_Artist_Then_The_Image_Url_Is_Returned(
        GetArtistPortImplementation implementation)
    {
        var imageUrl = "https://cdn.soundtrail.test/artists/artist-1005.jpg";
        await using var environment = await GetArtistPortContractTestEnvironment.ForExistingArtist(
            implementation,
            imageUrl: imageUrl);

        var result = await environment.Subject.GetArtistAsync(environment.ArtistId, CancellationToken.None);

        result!.ImageUrl.Should().Be(imageUrl);
    }
}
