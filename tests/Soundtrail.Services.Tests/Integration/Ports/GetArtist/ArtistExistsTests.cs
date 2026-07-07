namespace Soundtrail.Services.Tests.Integration.Ports.GetArtist;

public sealed class ArtistExistsTests
{
    public static TheoryData<GetArtistPortImplementation> Implementations => new()
    {
        GetArtistPortImplementation.Fake,
        GetArtistPortImplementation.Raven
    };

    [Theory]
    [MemberData(nameof(Implementations))]
    public async Task Given_An_Existing_Artist_When_Requesting_The_Artist_Then_An_Artist_Is_Returned(GetArtistPortImplementation implementation)
    {
        await using var environment = await GetArtistPortContractTestEnvironment.ForExistingArtist(implementation);

        var result = await environment.Subject.GetArtistAsync(environment.ArtistId, CancellationToken.None);

        result.Should().NotBeNull();
    }

    [Theory]
    [MemberData(nameof(Implementations))]
    public async Task Given_An_Existing_Artist_When_Requesting_The_Artist_Then_The_Artist_Id_Is_Returned(GetArtistPortImplementation implementation)
    {
        await using var environment = await GetArtistPortContractTestEnvironment.ForExistingArtist(implementation, artistId: "artist-1003");

        var result = await environment.Subject.GetArtistAsync(environment.ArtistId, CancellationToken.None);

        result!.ArtistId.Value.Should().Be("artist-1003");
    }

    [Theory]
    [MemberData(nameof(Implementations))]
    public async Task Given_An_Existing_Artist_When_Requesting_The_Artist_Then_The_Artist_Name_Is_Returned(GetArtistPortImplementation implementation)
    {
        await using var environment = await GetArtistPortContractTestEnvironment.ForExistingArtist(implementation, artistName: "Artist 1004");

        var result = await environment.Subject.GetArtistAsync(environment.ArtistId, CancellationToken.None);

        result!.ArtistName.Value.Should().Be("Artist 1004");
    }

    [Theory]
    [MemberData(nameof(Implementations))]
    public async Task Given_An_Existing_Artist_When_Requesting_The_Artist_Then_No_Description_Is_Returned(GetArtistPortImplementation implementation)
    {
        await using var environment = await GetArtistPortContractTestEnvironment.ForExistingArtist(implementation);

        var result = await environment.Subject.GetArtistAsync(environment.ArtistId, CancellationToken.None);

        result!.Description.Should().BeNull();
    }

    [Theory]
    [MemberData(nameof(Implementations))]
    public async Task Given_An_Existing_Artist_When_Requesting_The_Artist_Then_The_Image_Url_Is_Returned(GetArtistPortImplementation implementation)
    {
        await using var environment = await GetArtistPortContractTestEnvironment.ForExistingArtist(implementation, imageUrl: "https://cdn.soundtrail.test/artists/artist-1005.jpg");

        var result = await environment.Subject.GetArtistAsync(environment.ArtistId, CancellationToken.None);

        result!.ImageUrl.Should().Be("https://cdn.soundtrail.test/artists/artist-1005.jpg");
    }
}
