using System.Net;
using System.Net.Http.Json;
using FluentAssertions;

namespace Soundtrail.Services.Tests.Integration.Features.Search;

public sealed class SearchEndpointsTests(SoundtrailServicesApiFactory factory)
    : IClassFixture<SoundtrailServicesApiFactory>
{
    private readonly HttpClient client = factory.CreateClient();

    [Fact]
    public async Task Given_A_Known_Query_When_The_Search_Endpoint_Is_Called_Then_It_Returns_A_Local_Resolved_Result()
    {
        factory.TrackSearch.Seed(ApiKnownTracks.MrBrightside());
        var response = await this.client.GetAsync("/search?q=mr%20brightside");
        
        var content = await response.Content.ReadFromJsonAsync<SearchResponseContract>();

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        content.Should().NotBeNull();
        content!.Status.Should().Be("resolved");
        content.Results.Should().ContainSingle();
        content.Results[0].Title.Should().Be("Mr. Brightside");
    }

    [Fact]
    public async Task Given_An_Unknown_Query_When_The_Search_Endpoint_Is_Called_Then_It_Returns_Pending_And_Records_Demand()
    {
        factory.TrackSearch.Seed();
        var response = await this.client.GetAsync("/search?q=rare%20unknown%20song");
        var content = await response.Content.ReadFromJsonAsync<SearchResponseContract>();

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        content.Should().NotBeNull();
        content!.Status.Should().Be("pending");
        factory.LookupMusicRequests.EnqueuedRequests.Should().ContainSingle();
        factory.LookupMusicRequests.EnqueuedRequests[0].Query.Value.Should().Be("rare unknown song");
    }

    private sealed class SearchResponseContract
    {
        public string Status { get; set; } = string.Empty;

        public string Query { get; set; } = string.Empty;

        public List<SearchResultContract> Results { get; set; } = [];
    }

    private sealed class SearchResultContract
    {
        public string Title { get; set; } = string.Empty;
    }
}
