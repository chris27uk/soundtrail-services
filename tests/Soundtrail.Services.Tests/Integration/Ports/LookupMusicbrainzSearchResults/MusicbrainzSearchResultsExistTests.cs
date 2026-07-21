using Soundtrail.Domain.Catalog;
using Soundtrail.Domain.Search;

namespace Soundtrail.Services.Tests.Integration.Ports.LookupMusicbrainzSearchResults;

public sealed class MusicbrainzSearchResultsExistTests
{
    public static TheoryData<ReadCatalogEntriesBySearchCriteriaPortImplementation> Implementations => new()
    {
        ReadCatalogEntriesBySearchCriteriaPortImplementation.Fake,
        ReadCatalogEntriesBySearchCriteriaPortImplementation.WireMock
    };

    [Theory]
    [MemberData(nameof(Implementations))]
    public async Task Given_A_Musicbrainz_Search_When_Reading_Then_Catalog_Entries_Are_Returned(
        ReadCatalogEntriesBySearchCriteriaPortImplementation implementation)
    {
        using var environment = ReadCatalogEntriesBySearchCriteriaPortContractTestEnvironment.ForExistingResults(implementation);

        var result = await environment.Subject.ReadAsync(new SearchCriteria("rare song", SearchType.All), CancellationToken.None);

        result.Should().NotBeEmpty();
        result.Select(x => x.Item).Should().Contain(item => item is CatalogItem.MusicArtist);
        result.Select(x => x.Item).Should().Contain(item => item is CatalogItem.MusicAlbum);
        result.Select(x => x.Item).Should().Contain(item => item is CatalogItem.MusicTrack);
    }
}
