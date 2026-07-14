using Soundtrail.Domain.Catalog.Albums;

namespace Soundtrail.Services.Tests.Integration.Ports.GetAlbum;

public sealed class AlbumDoesNotExistTests
{
    public static TheoryData<GetAlbumPortImplementation> Implementations => new()
    {
        GetAlbumPortImplementation.Fake,
        GetAlbumPortImplementation.Raven
    };

    [Theory]
    [MemberData(nameof(Implementations))]
    public async Task Given_A_Missing_Album_When_Requesting_The_Album_Then_No_Album_Is_Returned(GetAlbumPortImplementation implementation)
    {
        await using var environment = await GetAlbumPortContractTestEnvironment.ForMissingAlbum(
            implementation,
            AlbumId.From("artist-907", "album-907"));

        var result = await environment.Subject.GetAlbumAsync(environment.AlbumId, CancellationToken.None);

        result.Should().BeNull();
    }
}
