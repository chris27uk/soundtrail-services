using FluentAssertions;
using System.Net;
using System.Net.Http.Json;
using Soundtrail.Domain.Search;
using Soundtrail.Services.Tests.Integration.Api.Features.Search;

namespace Soundtrail.Services.Tests.Integration.Api.WebApi.Search;

public sealed class SearchWebApiRoutingTests
{
    [Fact]
    public async Task Given_The_Search_Route_When_Requesting_A_Valid_Query_Then_It_Returns_Ok()
    {
        await using var env = SearchWebApiTestEnvironment.Create();
        env.SearchHandler.RespondWith(new SearchCatalogResponse("mr brightside", [], new SearchDiscovery(false, null, null)));

        var response = await env.Client.GetAsync("/search?q=mr%20brightside");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Given_An_Invalid_Search_Request_When_Requesting_The_Search_Route_Then_The_Error_Model_Is_Returned()
    {
        await using var env = SearchWebApiTestEnvironment.Create();
        env.SearchHandler.RespondWith(new SearchCatalogResponse("mr brightside", [], new SearchDiscovery(false, null, null)));

        var httpResponse = await env.Client.GetAsync("/search?q=mr%20brightside&limit=101");
        var response = await httpResponse.Content.ReadFromJsonAsync<ErrorResponseContract>();

        httpResponse.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        response!.Error.Should().Contain("Limit must be between");
    }

    private sealed class ErrorResponseContract
    {
        public string Error { get; set; } = string.Empty;
    }
}
