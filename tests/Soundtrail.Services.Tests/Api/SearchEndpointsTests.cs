using System.Net;
using System.Net.Http.Json;
using FluentAssertions;

namespace Soundtrail.Services.Tests.Api;

public sealed class SearchEndpointsTests : IClassFixture<SoundtrailServicesApiFactory>
{
    private readonly HttpClient _client;
    private readonly SoundtrailServicesApiFactory _factory;

    public SearchEndpointsTests(SoundtrailServicesApiFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task Known_Query_Returns_Local_Resolved_Result()
    {
        _factory.TrackSearch.Seed(ApiKnownTracks.MrBrightside());

        var response = await _client.GetAsync("/search?q=mr%20brightside");
        var content = await response.Content.ReadFromJsonAsync<SearchResponseContract>();

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        content.Should().NotBeNull();
        content!.Status.Should().Be("resolved");
        content.Results.Should().ContainSingle();
        content.Results[0].Title.Should().Be("Mr. Brightside");
    }

    [Fact]
    public async Task Unknown_Query_Returns_Pending_And_Records_Demand()
    {
        _factory.TrackSearch.Seed();

        var response = await _client.GetAsync("/search?q=rare%20unknown%20song");
        var content = await response.Content.ReadFromJsonAsync<SearchResponseContract>();

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        content.Should().NotBeNull();
        content!.Status.Should().Be("pending");
        content.QueryId.Should().StartWith("q_");
        _factory.DemandStore.RecordedQueries.Should().Contain("rare unknown song");
    }

    private sealed class SearchResponseContract
    {
        public string Status { get; set; } = string.Empty;

        public string Query { get; set; } = string.Empty;

        public string? QueryId { get; set; }

        public List<SearchResultContract> Results { get; set; } = [];
    }

    private sealed class SearchResultContract
    {
        public string Title { get; set; } = string.Empty;
    }
}
