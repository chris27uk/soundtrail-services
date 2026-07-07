using Soundtrail.Domain.Catalog;

namespace Soundtrail.Services.Tests.Integration.Ports.GetArtist;

public sealed class ArtistDoesNotExistTests
{
    public static TheoryData<GetArtistPortImplementation> Implementations => new()
    {
        GetArtistPortImplementation.Fake,
        GetArtistPortImplementation.Raven
    };

    [Theory]
    [MemberData(nameof(Implementations))]
    public async Task Given_A_Missing_Artist_When_Requesting_The_Artist_Then_No_Artist_Is_Returned(GetArtistPortImplementation implementation)
    {
        await using var environment = await GetArtistPortContractTestEnvironment.ForMissingArtist(
            implementation,
            ArtistId.From("artist-1006"));

        var result = await environment.Subject.GetArtistAsync(environment.ArtistId, CancellationToken.None);

        result.Should().BeNull();
    }
}
