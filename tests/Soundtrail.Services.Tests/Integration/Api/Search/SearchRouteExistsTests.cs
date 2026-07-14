using System.Net;

namespace Soundtrail.Services.Tests.Integration.Api.Search;

public sealed class SearchRouteExistsTests
{
    [Fact]
    public async Task Given_Existing_Search_Results_When_Searching_Then_Ok_Is_Returned()
    {
        using var environment = SearchRouteTestEnvironment.ForExistingSearchResults();

        var response = await environment.Client.GetAsync("/search?query=u2&filter=artist");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }
}
