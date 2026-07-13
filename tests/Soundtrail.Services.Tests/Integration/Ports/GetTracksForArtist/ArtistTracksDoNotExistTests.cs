using Soundtrail.Domain.Catalog;

namespace Soundtrail.Services.Tests.Integration.Ports.GetTracksForArtist;

public sealed class ArtistTracksDoNotExistTests
{
    public static TheoryData<GetTracksForArtistPortImplementation> Implementations => new()
    {
        GetTracksForArtistPortImplementation.Fake,
        GetTracksForArtistPortImplementation.Raven
    };

    [Theory]
    [MemberData(nameof(Implementations))]
    public async Task Given_Missing_Artist_Tracks_When_Requesting_The_Artist_Tracks_Then_No_Artist_Tracks_Are_Returned(GetTracksForArtistPortImplementation implementation)
    {
        await using var environment = await GetTracksForArtistPortContractTestEnvironment.ForMissingArtistTracks(implementation, ArtistId.From("artist-2702"));

        var result = await environment.Subject.GetTracksForArtistAsync(environment.ArtistId, CancellationToken.None);

        result.Should().BeNull();
    }
}
