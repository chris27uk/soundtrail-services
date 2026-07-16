using Soundtrail.Domain.Search;
using Soundtrail.Services.Api.Features.Search.Contract;

namespace Soundtrail.Services.Tests.Unit.Search;

public sealed class SearchResultsDoNotExistTests
{
    [Fact]
    public async Task Given_Missing_Search_Results_When_Searching_Then_No_Search_Results_Are_Returned()
    {
        var environment = SearchMissingUnitTestEnvironment.ForMissingSearch();

        var result = await environment.CreateSubjectUnderTest().Handle(environment.CreateRequest());

        result.Should().BeNull();
    }

    [Fact]
    public async Task Given_Missing_Search_Results_When_Searching_Then_The_Requested_Search_Criteria_Is_Read()
    {
        var environment = SearchMissingUnitTestEnvironment.ForMissingSearch(queryText: "u2", filter: SearchFilter.Artist);

        await environment.CreateSubjectUnderTest().Handle(environment.CreateRequest());

        environment.Port.RequestedSearchCriteria.Single().Should().Be(new SearchCriteria("u2", SearchFilter.Artist));
    }
}
