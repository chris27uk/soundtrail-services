using FluentAssertions;
using Soundtrail.Domain.Model;
using Soundtrail.Domain.Search;
using System.Net.Http.Json;

namespace Soundtrail.Services.Tests.Integration.Api.Features.Search.KnownLocalTrack;

public sealed class HttpRouteResponsesTests
{
    [Fact]
    public async Task Given_A_Search_Request_When_Searching_Then_Query_Parameters_Are_Bound_Into_The_Handler_Request()
    {
        await using var factory = SearchHttpRouteApiFactory.WithResolvedSearch("mr brightside", ApiKnownTracks.MrBrightsideCatalogTrack());
        var client = factory.CreateClient();

        await client.GetAsync("/search?q=mr%20brightside&types=track&playback=spotify,appleMusic&limit=5&offset=10");

        factory.SearchMusicHandler.Requests.Should().ContainSingle().Which.Should().BeEquivalentTo(
            new SearchCatalogCommand(
                NormalizedSearchQuery.FromText("mr brightside"),
                SearchTypesFilter.Parse("track"),
                PlaybackProviderFilter.Parse("spotify,appleMusic"),
                SearchLimit.From(5),
                SearchOffset.From(10)));
    }

    [Fact]
    public async Task Given_A_Resolved_Handler_Response_When_Searching_Then_Response_Query_Is_Mapped()
    {
        await using var factory = SearchHttpRouteApiFactory.WithResolvedSearch("mr brightside", ApiKnownTracks.MrBrightsideCatalogTrack());
        var client = factory.CreateClient();

        var response = await client.GetFromJsonAsync<SearchResponseContract>("/search?q=mr%20brightside");

        response!.Query.Should().Be("mr brightside");
    }

    [Fact]
    public async Task Given_A_Resolved_Handler_Response_When_Searching_Then_Response_Contains_A_Single_Result()
    {
        using var factory = SearchHttpRouteApiFactory.WithResolvedSearch("mr brightside", ApiKnownTracks.MrBrightsideCatalogTrack());
        var client = factory.CreateClient();

        var response = await client.GetFromJsonAsync<SearchResponseContract>("/search?q=mr%20brightside");

        response!.Results.Should().ContainSingle();
    }

    [Fact]
    public async Task Given_A_Resolved_Handler_Response_When_Searching_Then_Result_Type_Is_Mapped()
    {
        using var factory = SearchHttpRouteApiFactory.WithResolvedSearch("mr brightside", ApiKnownTracks.MrBrightsideCatalogTrack());
        var client = factory.CreateClient();

        var response = await client.GetFromJsonAsync<SearchResponseContract>("/search?q=mr%20brightside");

        response!.Results[0].Type.Should().Be("track");
    }

    [Fact]
    public async Task Given_A_Resolved_Handler_Response_When_Searching_Then_Result_Name_Is_Mapped()
    {
        using var factory = SearchHttpRouteApiFactory.WithResolvedSearch("mr brightside", ApiKnownTracks.MrBrightsideCatalogTrack());
        var client = factory.CreateClient();

        var response = await client.GetFromJsonAsync<SearchResponseContract>("/search?q=mr%20brightside");

        response!.Results[0].Name.Should().Be("Mr. Brightside");
    }

    [Fact]
    public async Task Given_A_Resolved_Handler_Response_When_Searching_Then_Discovery_Is_Not_Requesting_Work()
    {
        using var factory = SearchHttpRouteApiFactory.WithResolvedSearch("mr brightside", ApiKnownTracks.MrBrightsideCatalogTrack());
        var client = factory.CreateClient();

        var response = await client.GetFromJsonAsync<SearchResponseContract>("/search?q=mr%20brightside");

        response!.Discovery.WillBeLookedUp.Should().BeFalse();
    }

    private sealed class SearchResponseContract
    {
        public string Query { get; set; } = string.Empty;

        public List<SearchResultContract> Results { get; set; } = [];

        public SearchDiscoveryContract Discovery { get; set; } = new();
    }

    private sealed class SearchResultContract
    {
        public string Type { get; set; } = string.Empty;

        public string Name { get; set; } = string.Empty;
    }

    private sealed class SearchDiscoveryContract
    {
        public bool WillBeLookedUp { get; set; }
    }
}
