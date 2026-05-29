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
    public async Task Given_A_Query_With_Surrounding_Whitespace_When_The_Search_Endpoint_Is_Called_Then_It_Is_Trimmed_And_Resolved()
    {
        factory.TrackSearch.Seed(ApiKnownTracks.MrBrightside());
        var response = await this.client.GetAsync("/search?q=%20%20mr%20brightside%20%20");
        var content = await response.Content.ReadFromJsonAsync<SearchResponseContract>();

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        content.Should().NotBeNull();
        content!.Status.Should().Be("resolved");
        content.Query.Should().Be("mr brightside");
        content.Results.Should().ContainSingle();
    }

    [Fact]
    public async Task Given_A_Query_With_Punctuation_When_The_Search_Endpoint_Is_Called_Then_It_Is_Normalized_For_Matching()
    {
        factory.TrackSearch.Seed(ApiKnownTracks.MrBrightside());
        var response = await this.client.GetAsync("/search?q=Mr.%20Brightside!!!");
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
        content.QueryId.Should().StartWith("q_");
        factory.DemandStore.RecordedQueries.Should().Contain("rare unknown song");
    }

    [Fact]
    public async Task Given_A_Minimum_Confidence_When_The_Search_Endpoint_Is_Called_Then_Lower_Confidence_Results_Are_Filtered_Out()
    {
        factory.TrackSearch.Seed(
            ApiKnownTracks.SongWithConfidence("Confidence Song High", 0.95),
            ApiKnownTracks.SongWithConfidence("Confidence Song Low", 0.40));

        var response = await this.client.GetAsync("/search?q=confidence%20song&minConfidence=0.8");
        var content = await response.Content.ReadFromJsonAsync<SearchResponseContract>();

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        content.Should().NotBeNull();
        content!.Status.Should().Be("resolved");
        content.Results.Should().ContainSingle();
        content.Results[0].Title.Should().Be("Confidence Song High");
    }

    [Fact]
    public async Task Given_A_Missing_Query_When_The_Search_Endpoint_Is_Called_Then_It_Returns_Bad_Request()
    {
        var response = await this.client.GetAsync("/search");
        var content = await response.Content.ReadFromJsonAsync<ErrorResponseContract>();

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        content.Should().NotBeNull();
        content!.Error.Should().Contain("Search query is required");
    }

    [Fact]
    public async Task Given_A_Whitespace_Only_Query_When_The_Search_Endpoint_Is_Called_Then_It_Returns_Bad_Request()
    {
        var response = await this.client.GetAsync("/search?q=%20%20%20");
        var content = await response.Content.ReadFromJsonAsync<ErrorResponseContract>();

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        content.Should().NotBeNull();
        content!.Error.Should().Contain("Search query is required");
    }

    [Fact]
    public async Task Given_No_Limit_When_The_Search_Endpoint_Is_Called_Then_The_Default_Limit_Is_Applied()
    {
        factory.TrackSearch.Seed(
            ApiKnownTracks.Track("track 01"),
            ApiKnownTracks.Track("track 02"),
            ApiKnownTracks.Track("track 03"),
            ApiKnownTracks.Track("track 04"),
            ApiKnownTracks.Track("track 05"),
            ApiKnownTracks.Track("track 06"),
            ApiKnownTracks.Track("track 07"),
            ApiKnownTracks.Track("track 08"),
            ApiKnownTracks.Track("track 09"),
            ApiKnownTracks.Track("track 10"),
            ApiKnownTracks.Track("track 11"));

        var response = await this.client.GetAsync("/search?q=track");
        var content = await response.Content.ReadFromJsonAsync<SearchResponseContract>();

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        content.Should().NotBeNull();
        content!.Status.Should().Be("resolved");
        content.Results.Should().HaveCount(10);
    }

    [Fact]
    public async Task Given_An_Invalid_Limit_When_The_Search_Endpoint_Is_Called_Then_It_Returns_Bad_Request()
    {
        var response = await this.client.GetAsync("/search?q=mr%20brightside&limit=26");
        var content = await response.Content.ReadFromJsonAsync<ErrorResponseContract>();

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        content.Should().NotBeNull();
        content!.Error.Should().Contain("Limit must be between 1 and 25");
    }

    [Fact]
    public async Task Given_A_Zero_Limit_When_The_Search_Endpoint_Is_Called_Then_It_Returns_Bad_Request()
    {
        var response = await this.client.GetAsync("/search?q=mr%20brightside&limit=0");
        var content = await response.Content.ReadFromJsonAsync<ErrorResponseContract>();

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        content.Should().NotBeNull();
        content!.Error.Should().Contain("Limit must be between 1 and 25");
    }

    [Fact]
    public async Task Given_A_Negative_Minimum_Confidence_When_The_Search_Endpoint_Is_Called_Then_It_Returns_Bad_Request()
    {
        var response = await this.client.GetAsync("/search?q=mr%20brightside&minConfidence=-0.1");
        var content = await response.Content.ReadFromJsonAsync<ErrorResponseContract>();

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        content.Should().NotBeNull();
        content!.Error.Should().Contain("Confidence must be between 0 and 1");
    }

    [Fact]
    public async Task Given_A_Minimum_Confidence_Greater_Than_One_When_The_Search_Endpoint_Is_Called_Then_It_Returns_Bad_Request()
    {
        var response = await this.client.GetAsync("/search?q=mr%20brightside&minConfidence=1.1");
        var content = await response.Content.ReadFromJsonAsync<ErrorResponseContract>();

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        content.Should().NotBeNull();
        content!.Error.Should().Contain("Confidence must be between 0 and 1");
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

    private sealed class ErrorResponseContract
    {
        public string Error { get; set; } = string.Empty;
    }
}
