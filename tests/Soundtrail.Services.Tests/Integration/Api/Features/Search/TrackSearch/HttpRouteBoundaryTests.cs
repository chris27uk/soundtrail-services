using FluentAssertions;
using System.Net;
using System.Net.Http.Json;

namespace Soundtrail.Services.Tests.Integration.Api.Features.Search.TrackSearch;

public sealed class HttpRouteBoundaryTests
{
    [Fact]
    public async Task Given_An_Omitted_Limit_When_Searching_Then_The_Default_Limit_Is_Bound_Into_The_Handler_Request()
    {
        await using var factory = SearchHttpRouteApiFactory.WithResolvedSearch("mr brightside", ApiKnownTracks.MrBrightsideCatalogTrack());
        var client = factory.CreateClient();

        await client.GetAsync("/search?q=mr%20brightside");

        factory.SearchMusicHandler.Requests.Should().ContainSingle()
            .Which.Limit.Value.Should().Be(25);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(101)]
    public async Task Given_An_Out_Of_Range_Limit_When_Searching_Then_BadRequest_Is_Returned(int limit)
    {
        await using var factory = SearchHttpRouteApiFactory.WithResolvedSearch("mr brightside", ApiKnownTracks.MrBrightsideCatalogTrack());
        var client = factory.CreateClient();

        var response = await client.GetAsync($"/search?q=mr%20brightside&limit={limit}");

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Given_A_Negative_Offset_When_Searching_Then_BadRequest_Is_Returned()
    {
        await using var factory = SearchHttpRouteApiFactory.WithResolvedSearch("mr brightside", ApiKnownTracks.MrBrightsideCatalogTrack());
        var client = factory.CreateClient();

        var response = await client.GetAsync("/search?q=mr%20brightside&offset=-1");

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Given_An_Out_Of_Range_Limit_When_Searching_Then_The_Error_Message_Is_Returned()
    {
        await using var factory = SearchHttpRouteApiFactory.WithResolvedSearch("mr brightside", ApiKnownTracks.MrBrightsideCatalogTrack());
        var client = factory.CreateClient();

        var httpResponse = await client.GetAsync("/search?q=mr%20brightside&limit=101");
        var response = await httpResponse.Content.ReadFromJsonAsync<ErrorResponseContract>();

        response!.Error.Should().Contain("Limit must be between");
    }

    [Fact]
    public async Task Given_A_Negative_Offset_When_Searching_Then_The_Error_Message_Is_Returned()
    {
        await using var factory = SearchHttpRouteApiFactory.WithResolvedSearch("mr brightside", ApiKnownTracks.MrBrightsideCatalogTrack());
        var client = factory.CreateClient();

        var httpResponse = await client.GetAsync("/search?q=mr%20brightside&offset=-1");
        var response = await httpResponse.Content.ReadFromJsonAsync<ErrorResponseContract>();

        response!.Error.Should().Contain("Offset must be zero or greater");
    }

    [Fact]
    public async Task Given_An_Unknown_Search_Type_When_Searching_Then_BadRequest_Is_Returned()
    {
        await using var factory = SearchHttpRouteApiFactory.WithResolvedSearch("mr brightside", ApiKnownTracks.MrBrightsideCatalogTrack());
        var client = factory.CreateClient();

        var response = await client.GetAsync("/search?q=mr%20brightside&types=playlist");

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Given_An_Unknown_Playback_Provider_When_Searching_Then_BadRequest_Is_Returned()
    {
        await using var factory = SearchHttpRouteApiFactory.WithResolvedSearch("mr brightside", ApiKnownTracks.MrBrightsideCatalogTrack());
        var client = factory.CreateClient();

        var response = await client.GetAsync("/search?q=mr%20brightside&playback=tidal");

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    private sealed class ErrorResponseContract
    {
        public string Error { get; set; } = string.Empty;
    }
}
