using Soundtrail.Domain.Catalog;
using Soundtrail.Domain.Catalog.Tracks;
using Soundtrail.Domain.Search;

namespace Soundtrail.Services.Tests.Integration.Ports.Search;

public sealed class SearchResultsExistTests
{
    public static TheoryData<SearchPortImplementation> Implementations => new()
    {
        SearchPortImplementation.Fake,
        SearchPortImplementation.Raven
    };

    [Theory]
    [MemberData(nameof(Implementations))]
    public async Task Given_Existing_Search_Results_When_Searching_Then_Search_Results_Are_Returned(SearchPortImplementation implementation)
    {
        await using var environment = await SearchPortContractTestEnvironment.ForExistingResults(implementation);

        var result = await environment.Subject.SearchAsync(environment.SearchCriteria, CancellationToken.None);

        result.Should().NotBeNull();
    }

    [Theory]
    [MemberData(nameof(Implementations))]
    public async Task Given_Existing_Search_Results_When_Searching_Then_The_Query_Text_Is_Returned(SearchPortImplementation implementation)
    {
        await using var environment = await SearchPortContractTestEnvironment.ForExistingResults(implementation, queryText: "abba");

        var result = await environment.Subject.SearchAsync(environment.SearchCriteria, CancellationToken.None);

        result!.QueryText.Should().Be("abba");
    }

    [Theory]
    [MemberData(nameof(Implementations))]
    public async Task Given_Existing_Search_Results_When_Searching_Then_The_Filter_Is_Returned(SearchPortImplementation implementation)
    {
        await using var environment = await SearchPortContractTestEnvironment.ForExistingResults(implementation, filter: SearchType.Album, musicCatalogId: "artist-3101:album-3201", resultType: SearchType.Album);

        var result = await environment.Subject.SearchAsync(environment.SearchCriteria, CancellationToken.None);

        result!.Filter.Should().Be(SearchType.Album);
    }

    [Theory]
    [MemberData(nameof(Implementations))]
    public async Task Given_Existing_Search_Results_When_Searching_Then_The_Results_Are_Returned(SearchPortImplementation implementation)
    {
        await using var environment = await SearchPortContractTestEnvironment.ForExistingResults(implementation);

        var result = await environment.Subject.SearchAsync(environment.SearchCriteria, CancellationToken.None);

        result!.Results.Should().HaveCount(1);
    }

    [Theory]
    [MemberData(nameof(Implementations))]
    public async Task Given_Existing_Search_Results_When_Searching_Then_The_Music_Catalog_Id_Is_Returned(SearchPortImplementation implementation)
    {
        await using var environment = await SearchPortContractTestEnvironment.ForExistingResults(implementation, filter: SearchType.Track, musicCatalogId: global::Soundtrail.Services.Tests.TestTrackIds.Value("track-3103"), resultType: SearchType.Track);

        var result = await environment.Subject.SearchAsync(environment.SearchCriteria, CancellationToken.None);

        result!.Results[0].MusicCatalogId.Should().Be(new CatalogItemId.Track(global::Soundtrail.Services.Tests.TestTrackIds.Create("track-3103")));
    }

    [Theory]
    [MemberData(nameof(Implementations))]
    public async Task Given_Existing_Search_Results_When_Searching_Then_The_Result_Type_Is_Returned(SearchPortImplementation implementation)
    {
        await using var environment = await SearchPortContractTestEnvironment.ForExistingResults(implementation, filter: SearchType.Track, musicCatalogId: global::Soundtrail.Services.Tests.TestTrackIds.Value("track-3104"), resultType: SearchType.Track);

        var result = await environment.Subject.SearchAsync(environment.SearchCriteria, CancellationToken.None);

        result!.Results[0].ResultType.Should().Be(SearchType.Track);
    }

    [Theory]
    [MemberData(nameof(Implementations))]
    public async Task Given_Existing_Search_Results_When_Searching_Then_The_Title_Is_Returned(SearchPortImplementation implementation)
    {
        await using var environment = await SearchPortContractTestEnvironment.ForExistingResults(implementation, title: "Greatest Hits");

        var result = await environment.Subject.SearchAsync(environment.SearchCriteria, CancellationToken.None);

        result!.Results[0].Title.Should().Be("Greatest Hits");
    }

    [Theory]
    [MemberData(nameof(Implementations))]
    public async Task Given_Existing_Search_Results_When_Searching_Then_The_Artist_Name_Is_Returned(SearchPortImplementation implementation)
    {
        await using var environment = await SearchPortContractTestEnvironment.ForExistingResults(implementation, artistName: "The Artist");

        var result = await environment.Subject.SearchAsync(environment.SearchCriteria, CancellationToken.None);

        result!.Results[0].ArtistName.Should().Be("The Artist");
    }

    [Theory]
    [MemberData(nameof(Implementations))]
    public async Task Given_Existing_Search_Results_When_Searching_Then_The_Album_Title_Is_Returned(SearchPortImplementation implementation)
    {
        await using var environment = await SearchPortContractTestEnvironment.ForExistingResults(implementation, albumTitle: "The Album");

        var result = await environment.Subject.SearchAsync(environment.SearchCriteria, CancellationToken.None);

        result!.Results[0].AlbumTitle.Should().Be("The Album");
    }

    [Theory]
    [MemberData(nameof(Implementations))]
    public async Task Given_Existing_Search_Results_When_Searching_Then_The_Artwork_Url_Is_Returned(SearchPortImplementation implementation)
    {
        await using var environment = await SearchPortContractTestEnvironment.ForExistingResults(implementation, artworkUrl: "https://cdn.soundtrail.test/albums/album-3101.jpg");

        var result = await environment.Subject.SearchAsync(environment.SearchCriteria, CancellationToken.None);

        result!.Results[0].ArtworkUrl.Should().Be("https://cdn.soundtrail.test/albums/album-3101.jpg");
    }
}
