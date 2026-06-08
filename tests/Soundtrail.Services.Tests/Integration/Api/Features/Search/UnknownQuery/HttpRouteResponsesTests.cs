using FluentAssertions;
using System.Net.Http.Json;

namespace Soundtrail.Services.Tests.Integration.Api.Features.Search.UnknownQuery;

public sealed class HttpRouteResponsesTests
{
    [Fact]
    public async Task Given_A_Pending_Handler_Response_When_Searching_Then_Response_Status_Is_Mapped()
    {
        using var factory = SearchHttpRouteApiFactory.WithPendingSearch("rare unknown song");
        var client = factory.CreateClient();

        var response = await client.GetFromJsonAsync<SearchResponseContract>("/search?q=rare%20unknown%20song");

        response!.Status.Should().Be("pending");
    }

    [Fact]
    public async Task Given_A_Pending_Handler_Response_When_Searching_Then_Response_RetryAfterSeconds_Is_Mapped()
    {
        using var factory = SearchHttpRouteApiFactory.WithPendingSearch("rare unknown song");
        var client = factory.CreateClient();

        var response = await client.GetFromJsonAsync<SearchResponseContract>("/search?q=rare%20unknown%20song");

        response!.RetryAfterSeconds.Should().Be(60);
    }

    private sealed class SearchResponseContract
    {
        public string Status { get; set; } = string.Empty;

        public int? RetryAfterSeconds { get; set; }
    }
}
