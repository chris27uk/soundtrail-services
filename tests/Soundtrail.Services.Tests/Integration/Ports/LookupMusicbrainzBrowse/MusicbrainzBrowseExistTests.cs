using Soundtrail.Domain.Catalog;

namespace Soundtrail.Services.Tests.Integration.Ports.LookupMusicbrainzBrowse;

public sealed class MusicbrainzBrowseExistTests
{
    public static TheoryData<ReadMusicbrainzBrowsePortImplementation> Implementations => new()
    {
        ReadMusicbrainzBrowsePortImplementation.Fake,
        ReadMusicbrainzBrowsePortImplementation.WireMock
    };

    [Theory]
    [MemberData(nameof(Implementations))]
    public async Task Given_Artist_Albums_When_Reading_Then_Albums_Are_Returned(ReadMusicbrainzBrowsePortImplementation implementation)
    {
        using var environment = ReadMusicbrainzBrowsePortContractTestEnvironment.ForExistingResults(implementation);

        var result = await environment.AlbumsPort.ReadAsync(environment.ArtistId, CancellationToken.None);

        result.Select(x => x.Item).Should().Contain(item => item is CatalogItem.MusicAlbum);
    }

    [Theory]
    [MemberData(nameof(Implementations))]
    public async Task Given_Artist_Tracks_When_Reading_Then_Tracks_Are_Returned(ReadMusicbrainzBrowsePortImplementation implementation)
    {
        using var environment = ReadMusicbrainzBrowsePortContractTestEnvironment.ForExistingResults(implementation);

        var result = await environment.ArtistTracksPort.ReadAsync(environment.ArtistId, CancellationToken.None);

        result.Select(x => x.Item).Should().Contain(item => item is CatalogItem.MusicTrack);
    }

    [Theory]
    [MemberData(nameof(Implementations))]
    public async Task Given_Album_Tracks_When_Reading_Then_Tracks_Are_Returned(ReadMusicbrainzBrowsePortImplementation implementation)
    {
        using var environment = ReadMusicbrainzBrowsePortContractTestEnvironment.ForExistingResults(implementation);

        var result = await environment.AlbumTracksPort.ReadAsync(environment.AlbumId, CancellationToken.None);

        result.Select(x => x.Item).Should().Contain(item => item is CatalogItem.MusicTrack);
    }
}
