using FluentAssertions;
using Soundtrail.Services.Features.Search;
using Soundtrail.Services.Features.Search.Models;
using System.Net.Http.Json;

namespace Soundtrail.Services.Tests.Api.Integration.HttpRoutes.Search.KnownLocalTrack;

public sealed class HttpRouteResponsesTests
{
    [Fact]
    public async Task Given_A_Search_Request_When_Searching_Then_Query_Parameters_Are_Bound_Into_The_Handler_Request()
    {
        await using var factory = SearchHttpRouteApiFactory.WithResolvedSearch("mr brightside", ApiKnownTracks.MrBrightside());
        var client = factory.CreateClient();

        await client.GetAsync("/search?q=mr%20brightside&limit=5&minConfidence=0.95");

        factory.SearchMusicHandler.Requests.Should().ContainSingle().Which.Should().BeEquivalentTo(
            new SearchMusicRequest(
                SearchQuery.From("mr brightside"),
                Limit.From(5),
                ConfidenceScore.From(0.95)));
    }

    [Fact]
    public async Task Given_A_Resolved_Handler_Response_When_Searching_Then_Response_Status_Is_Mapped()
    {
        await using var factory = SearchHttpRouteApiFactory.WithResolvedSearch("mr brightside", ApiKnownTracks.MrBrightside());
        var client = factory.CreateClient();

        var response = await client.GetFromJsonAsync<SearchResponseContract>("/search?q=mr%20brightside");

        response!.Status.Should().Be("resolved");
    }

    [Fact]
    public async Task Given_A_Resolved_Handler_Response_When_Searching_Then_Response_Source_Is_Mapped()
    {
        await using var factory = SearchHttpRouteApiFactory.WithResolvedSearch("mr brightside", ApiKnownTracks.MrBrightside());
        var client = factory.CreateClient();

        var response = await client.GetFromJsonAsync<SearchResponseContract>("/search?q=mr%20brightside");

        response!.Source.Should().Be("local");
    }

    [Fact]
    public async Task Given_A_Resolved_Handler_Response_When_Searching_Then_Response_Query_Is_Mapped()
    {
        await using var factory = SearchHttpRouteApiFactory.WithResolvedSearch("mr brightside", ApiKnownTracks.MrBrightside());
        var client = factory.CreateClient();

        var response = await client.GetFromJsonAsync<SearchResponseContract>("/search?q=mr%20brightside");

        response!.Query.Should().Be("mr brightside");
    }

    [Fact]
    public async Task Given_A_Resolved_Handler_Response_When_Searching_Then_Response_Contains_A_Single_Result()
    {
        using var factory = SearchHttpRouteApiFactory.WithResolvedSearch("mr brightside", ApiKnownTracks.MrBrightside());
        var client = factory.CreateClient();

        var response = await client.GetFromJsonAsync<SearchResponseContract>("/search?q=mr%20brightside");

        response!.Results.Should().ContainSingle();
    }

    [Fact]
    public async Task Given_A_Resolved_Handler_Response_When_Searching_Then_Result_Title_Is_Mapped()
    {
        using var factory = SearchHttpRouteApiFactory.WithResolvedSearch("mr brightside", ApiKnownTracks.MrBrightside());
        var client = factory.CreateClient();

        var response = await client.GetFromJsonAsync<SearchResponseContract>("/search?q=mr%20brightside");

        response!.Results[0].Title.Should().Be("Mr. Brightside");
    }

    private sealed class SearchResponseContract
    {
        public string Status { get; set; } = string.Empty;

        public string Source { get; set; } = string.Empty;

        public string Query { get; set; } = string.Empty;

        public List<SearchResultContract> Results { get; set; } = [];
    }

    private sealed class SearchResultContract
    {
        public string Title { get; set; } = string.Empty;

        public string Artist { get; set; } = string.Empty;

        public string? Isrc { get; set; }

        public string? Mbid { get; set; }

        public string? AppleId { get; set; }

        public string? SpotifyId { get; set; }
    }
}
