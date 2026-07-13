using Soundtrail.Domain.Catalog;

namespace Soundtrail.Services.Tests.Integration.Ports.GetAlbumsForArtist;

public sealed class ArtistAlbumsDoNotExistTests
{
    public static TheoryData<GetAlbumsForArtistPortImplementation> Implementations => new()
    {
        GetAlbumsForArtistPortImplementation.Fake,
        GetAlbumsForArtistPortImplementation.Raven
    };

    [Theory]
    [MemberData(nameof(Implementations))]
    public async Task Given_Missing_Artist_Albums_When_Requesting_The_Artist_Albums_Then_No_Artist_Albums_Are_Returned(GetAlbumsForArtistPortImplementation implementation)
    {
        await using var environment = await GetAlbumsForArtistPortContractTestEnvironment.ForMissingArtistAlbums(implementation, ArtistId.From("artist-2102"));

        var result = await environment.Subject.GetAlbumsForArtistAsync(environment.ArtistId, CancellationToken.None);

        result.Should().BeNull();
    }
}
