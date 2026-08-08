using Soundtrail.Domain.Catalog.Artists;
using Soundtrail.Services.Tests.Integration.Features.GetTracksForArtist;

namespace Soundtrail.Services.Tests.Integration.Features.GetTracksForArtist.Scenarios.Api.Ports;

public sealed class ArtistTracksDoNotExistTests
{
    public static TheoryData<GetTracksForArtistPortImplementation> Implementations => new()
    {
        GetTracksForArtistPortImplementation.Raven
    };

    [Theory]
    [MemberData(nameof(Implementations))]
    public async Task Given_Missing_Artist_Tracks_When_Requesting_The_Artist_Tracks_Then_No_Artist_Tracks_Are_Returned(
        GetTracksForArtistPortImplementation implementation)
    {
        var artistId = ArtistId.From("artist-2702");
        await using var environment = await GetTracksForArtistPortContractTestEnvironment.ForMissingArtistTracks(
            implementation,
            artistId);

        var result = await environment.Subject.GetTracksForArtistAsync(environment.ArtistId, CancellationToken.None);

        result.Should().BeNull();
    }
}
