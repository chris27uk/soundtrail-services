using Soundtrail.Domain.Catalog.Albums;

namespace Soundtrail.Services.Tests.Integration.Ports.GetTracksForAlbum;

public sealed class AlbumTracksDoNotExistTests
{
    public static TheoryData<GetTracksForAlbumPortImplementation> Implementations => new()
    {
        GetTracksForAlbumPortImplementation.Fake,
        GetTracksForAlbumPortImplementation.Raven
    };

    [Theory]
    [MemberData(nameof(Implementations))]
    public async Task Given_Missing_Album_Tracks_When_Requesting_The_Album_Tracks_Then_No_Album_Tracks_Are_Returned(GetTracksForAlbumPortImplementation implementation)
    {
        await using var environment = await GetTracksForAlbumPortContractTestEnvironment.ForMissingAlbumTracks(
            implementation,
            AlbumId.From("artist-1107", "album-1207"));

        var result = await environment.Subject.GetTracksForAlbumAsync(environment.AlbumId, CancellationToken.None);

        result.Should().BeNull();
    }
}
