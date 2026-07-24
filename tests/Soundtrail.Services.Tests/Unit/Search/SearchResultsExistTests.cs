using Soundtrail.Domain.Catalog;
using Soundtrail.Domain.Discovery;
using Soundtrail.Domain.Discovery.Events;
using Soundtrail.Domain.Search;
using Soundtrail.Contracts.Common;
using Soundtrail.Domain.Catalog.Tracks;
using Soundtrail.Domain.Common;
using Soundtrail.Services.Api.Features.Catalog.Search.Contract;
using Soundtrail.Services.Api.Features.Catalog.Shared.Contract;

namespace Soundtrail.Services.Tests.Unit.Search;

public sealed class SearchResultsExistTests
{
    [Fact]
    public async Task Given_Existing_Search_Results_When_Searching_Then_The_Port_Response_Is_Returned()
    {
        var response = SearchResults.CreateResponse();
        var environment = SearchUnitTestEnvironment.ForSearch(response: response);

        var result = await environment.CreateSubjectUnderTest().Handle(environment.CreateRequest());

        result.Should().NotBeSameAs(response);
        result.Should().BeEquivalentTo(response);
    }

    [Fact]
    public async Task Given_Existing_Search_Results_When_Searching_Then_The_Query_Text_Is_Returned()
    {
        var environment = SearchUnitTestEnvironment.ForSearch(response: SearchResults.CreateResponse(queryText: "abba"));

        var result = await environment.CreateSubjectUnderTest().Handle(environment.CreateRequest());

        result!.QueryText.Should().Be("abba");
    }

    [Fact]
    public async Task Given_Existing_Search_Results_When_Searching_Then_The_Filter_Is_Returned()
    {
        var environment = SearchUnitTestEnvironment.ForSearch(filter: SearchType.Album, response: SearchResults.CreateResponse(filter: SearchType.Album));

        var result = await environment.CreateSubjectUnderTest().Handle(environment.CreateRequest());

        result!.Filter.Should().Be(SearchType.Album);
    }

    [Fact]
    public async Task Given_Existing_Search_Results_When_Searching_Then_The_Results_Are_Returned()
    {
        var environment = SearchUnitTestEnvironment.ForSearch();

        var result = await environment.CreateSubjectUnderTest().Handle(environment.CreateRequest());

        result!.Results.Should().HaveCount(1);
    }

    [Fact]
    public async Task Given_Existing_Search_Results_When_Searching_Then_The_Music_Catalog_Id_Is_Returned()
    {
        var musicCatalogId = new CatalogItemId.Track(global::Soundtrail.Services.Tests.TestTrackIds.Create("track-2903"));
        var environment = SearchUnitTestEnvironment.ForSearch(
            filter: SearchType.Track,
            response: SearchResults.CreateResponse(filter: SearchType.Track, musicCatalogId: musicCatalogId, resultType: SearchType.Track));

        var result = await environment.CreateSubjectUnderTest().Handle(environment.CreateRequest());

        result!.Results[0].MusicCatalogId.Should().Be(musicCatalogId);
    }

    [Fact]
    public async Task Given_Existing_Search_Results_When_Searching_Then_The_Result_Type_Is_Returned()
    {
        var environment = SearchUnitTestEnvironment.ForSearch(
            filter: SearchType.Track,
            response: SearchResults.CreateResponse(filter: SearchType.Track, resultType: SearchType.Track));

        var result = await environment.CreateSubjectUnderTest().Handle(environment.CreateRequest());

        result!.Results[0].ResultType.Should().Be(SearchType.Track);
    }

    [Fact]
    public async Task Given_Existing_Search_Results_When_Searching_Then_The_Title_Is_Returned()
    {
        var environment = SearchUnitTestEnvironment.ForSearch(response: SearchResults.CreateResponse(title: "Greatest Hits"));

        var result = await environment.CreateSubjectUnderTest().Handle(environment.CreateRequest());

        result!.Results[0].Title.Should().Be("Greatest Hits");
    }

    [Fact]
    public async Task Given_Existing_Search_Results_When_Searching_Then_The_Artist_Name_Is_Returned()
    {
        var environment = SearchUnitTestEnvironment.ForSearch(response: SearchResults.CreateResponse(artistName: "The Artist"));

        var result = await environment.CreateSubjectUnderTest().Handle(environment.CreateRequest());

        result!.Results[0].ArtistName.Should().Be("The Artist");
    }

    [Fact]
    public async Task Given_Existing_Search_Results_When_Searching_Then_The_Album_Title_Is_Returned()
    {
        var environment = SearchUnitTestEnvironment.ForSearch(response: SearchResults.CreateResponse(albumTitle: "The Album"));

        var result = await environment.CreateSubjectUnderTest().Handle(environment.CreateRequest());

        result!.Results[0].AlbumTitle.Should().Be("The Album");
    }

    [Fact]
    public async Task Given_Existing_Search_Results_When_Searching_Then_The_Artwork_Url_Is_Returned()
    {
        var environment = SearchUnitTestEnvironment.ForSearch(response: SearchResults.CreateResponse(artworkUrl: "https://cdn.soundtrail.test/albums/album-2901.jpg"));

        var result = await environment.CreateSubjectUnderTest().Handle(environment.CreateRequest());

        result!.Results[0].ArtworkUrl.Should().Be("https://cdn.soundtrail.test/albums/album-2901.jpg");
    }

    [Fact]
    public async Task Given_Existing_Search_Results_When_Searching_Then_The_Requested_Search_Criteria_Is_Read()
    {
        var environment = SearchUnitTestEnvironment.ForSearch(queryText: "u2", filter: SearchType.Artist);

        await environment.CreateSubjectUnderTest().Handle(environment.CreateRequest());

        environment.Port.RequestedSearchCriteria.Single().Should().Be(new SearchCriteria("u2", SearchType.Artist));
    }

    [Fact]
    public async Task Given_Existing_Search_Results_When_Searching_Then_A_Search_Command_Is_Sent()
    {
        var environment = SearchUnitTestEnvironment.ForSearch();

        await environment.CreateSubjectUnderTest().Handle(environment.CreateRequest());

        environment.CommandBus.Commands.Should().ContainSingle();
    }

    [Fact]
    public async Task Given_Existing_Search_Results_When_Searching_Then_The_Search_Command_Filter_Is_Search_Criteria_Based()
    {
        var environment = SearchUnitTestEnvironment.ForSearch(queryText: "u2", filter: SearchType.Artist);

        await environment.CreateSubjectUnderTest().Handle(environment.CreateRequest());

        environment.CommandBus.Commands.Single().SearchCriteria.Should().Be(new SearchCriteria("u2", SearchType.Artist));
    }

    [Fact]
    public async Task Given_Existing_Search_Results_When_Searching_Then_The_Search_Command_Has_High_Priority()
    {
        var environment = SearchUnitTestEnvironment.ForSearch();

        await environment.CreateSubjectUnderTest().Handle(environment.CreateRequest());

        environment.CommandBus.Commands.Single().Priority.Should().Be(LookupPriorityBand.High);
    }

    [Fact]
    public async Task Given_Existing_Search_Results_When_Searching_Then_The_Search_Command_Trust_Level_Is_One_Hundred()
    {
        var environment = SearchUnitTestEnvironment.ForSearch();

        await environment.CreateSubjectUnderTest().Handle(environment.CreateRequest());

        environment.CommandBus.Commands.Single().TrustLevel.Should().Be(100);
    }

    [Fact]
    public async Task Given_Existing_Search_Results_When_Searching_Then_The_Search_Command_Risk_Score_Is_Zero()
    {
        var environment = SearchUnitTestEnvironment.ForSearch();

        await environment.CreateSubjectUnderTest().Handle(environment.CreateRequest());

        environment.CommandBus.Commands.Single().RiskScore.Should().Be(0);
    }

    [Fact]
    public async Task Given_Existing_Search_Results_When_Searching_Then_The_Search_Command_Requested_At_Is_Set()
    {
        var environment = SearchUnitTestEnvironment.ForSearch();

        await environment.CreateSubjectUnderTest().Handle(environment.CreateRequest());

        environment.CommandBus.Commands.Single().RequestedAt.Should().Be(environment.Clock.UtcNow);
    }

    [Fact]
    public async Task Given_Existing_Search_Results_When_Searching_Then_Discovery_Feedback_Is_Requested_For_The_Search_Target()
    {
        var environment = SearchUnitTestEnvironment.ForSearch(queryText: "u2", filter: SearchType.Artist);

        await environment.CreateSubjectUnderTest().Handle(environment.CreateRequest());

        environment.DiscoveryFeedbackPort.RequestedTarget
            .Should().Be(new EnrichmentTarget.SearchForUnknownCatalogItem(new SearchCriteria("u2", SearchType.Artist)));
    }

    [Fact]
    public async Task Given_Stored_Discovery_Feedback_When_Searching_Then_It_Is_Returned()
    {
        var environment = SearchUnitTestEnvironment.ForSearch();
        environment.DiscoveryFeedbackPort.Response = new DiscoveryFeedbackResponse(
            "scheduled",
            LookupPriorityBand.High,
            new DateTimeOffset(2026, 7, 18, 9, 0, 0, TimeSpan.Zero),
            new DateTimeOffset(2026, 7, 18, 9, 2, 0, TimeSpan.Zero),
            "Equivalent work is scheduled.",
            new DateTimeOffset(2026, 7, 18, 8, 59, 0, TimeSpan.Zero));

        var result = await environment.CreateSubjectUnderTest().Handle(environment.CreateRequest());

        result!.Discovery.Should().Be(environment.DiscoveryFeedbackPort.Response);
    }
}
