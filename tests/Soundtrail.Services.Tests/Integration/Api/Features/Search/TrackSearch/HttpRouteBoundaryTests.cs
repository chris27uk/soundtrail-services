using FluentAssertions;
using System.Net;
using System.Net.Http.Json;

namespace Soundtrail.Services.Tests.Integration.Api.Features.Search.TrackSearch;

public sealed class HttpRouteBoundaryTests
{
    [Fact]
    public async Task Given_An_Omitted_Limit_When_Searching_Then_The_Default_Limit_Is_Bound_Into_The_Handler_Request()
    {
        await using var factory = SearchHttpRouteApiFactory.WithResolvedSearch("mr brightside", ApiKnownTracks.MrBrightside());
        var client = factory.CreateClient();

        await client.GetAsync("/search?q=mr%20brightside");

        factory.SearchMusicHandler.Requests.Should().ContainSingle()
            .Which.Limit.Value.Should().Be(10);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(26)]
    public async Task Given_An_Out_Of_Range_Limit_When_Searching_Then_BadRequest_Is_Returned(int limit)
    {
        await using var factory = SearchHttpRouteApiFactory.WithResolvedSearch("mr brightside", ApiKnownTracks.MrBrightside());
        var client = factory.CreateClient();

        var response = await client.GetAsync($"/search?q=mr%20brightside&limit={limit}");

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Theory]
    [InlineData(-0.01)]
    [InlineData(1.01)]
    public async Task Given_An_Out_Of_Range_Minimum_Confidence_When_Searching_Then_BadRequest_Is_Returned(double minConfidence)
    {
        await using var factory = SearchHttpRouteApiFactory.WithResolvedSearch("mr brightside", ApiKnownTracks.MrBrightside());
        var client = factory.CreateClient();

        var response = await client.GetAsync($"/search?q=mr%20brightside&minConfidence={minConfidence}");

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Given_An_Out_Of_Range_Limit_When_Searching_Then_The_Error_Message_Is_Returned()
    {
        await using var factory = SearchHttpRouteApiFactory.WithResolvedSearch("mr brightside", ApiKnownTracks.MrBrightside());
        var client = factory.CreateClient();

        var httpResponse = await client.GetAsync("/search?q=mr%20brightside&limit=26");
        var response = await httpResponse.Content.ReadFromJsonAsync<ErrorResponseContract>();

        response!.Error.Should().Contain("Limit must be between");
    }

    [Fact]
    public async Task Given_An_Out_Of_Range_Minimum_Confidence_When_Searching_Then_The_Error_Message_Is_Returned()
    {
        await using var factory = SearchHttpRouteApiFactory.WithResolvedSearch("mr brightside", ApiKnownTracks.MrBrightside());
        var client = factory.CreateClient();

        var httpResponse = await client.GetAsync("/search?q=mr%20brightside&minConfidence=1.01");
        var response = await httpResponse.Content.ReadFromJsonAsync<ErrorResponseContract>();

        response!.Error.Should().Contain("Confidence must be between 0 and 1");
    }

    private sealed class ErrorResponseContract
    {
        public string Error { get; set; } = string.Empty;
    }
}
