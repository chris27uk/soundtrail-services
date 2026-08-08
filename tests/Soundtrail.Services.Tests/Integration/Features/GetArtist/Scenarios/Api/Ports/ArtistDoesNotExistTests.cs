using Soundtrail.Domain.Catalog.Artists;
using Soundtrail.Services.Tests.Integration.Features.GetArtist;

namespace Soundtrail.Services.Tests.Integration.Features.GetArtist.Scenarios.Api.Ports;

public sealed class ArtistDoesNotExistTests
{
    public static TheoryData<GetArtistPortImplementation> Implementations => new()
    {
        GetArtistPortImplementation.Raven
    };

    [Theory]
    [MemberData(nameof(Implementations))]
    public async Task Given_A_Missing_Artist_When_Requesting_The_Artist_Then_No_Artist_Is_Returned(
        GetArtistPortImplementation implementation)
    {
        var artistId = ArtistId.From("artist-1006");
        await using var environment = await GetArtistPortContractTestEnvironment.ForMissingArtist(
            implementation,
            artistId);

        var result = await environment.Subject.GetArtistAsync(environment.ArtistId, CancellationToken.None);

        result.Should().BeNull();
    }
}
