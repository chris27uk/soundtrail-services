using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Soundtrail.Services.Tests.Integration.Features.Search;

namespace Soundtrail.Services.Tests.Integration.Features.CatalogLookup;

public sealed class CatalogLookupEndpointsTests(SoundtrailServicesApiFactory factory)
    : IClassFixture<SoundtrailServicesApiFactory>
{
    private readonly HttpClient client = factory.CreateClient();

    [Fact]
    public async Task Given_A_Known_Isrc_When_The_Lookup_Endpoint_Is_Called_Then_It_Returns_The_Matching_Track()
    {
        factory.CatalogLookup.Seed(ApiKnownTracks.MrBrightsideTrack());

        var response = await this.client.GetAsync("/lookup?isrc=USIR20400274");
        var content = await response.Content.ReadFromJsonAsync<CatalogLookupResponseContract>();

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        content.Should().NotBeNull();
        content!.Track.Should().NotBeNull();
        content.Track!.Title.Should().Be("Mr. Brightside");
        content.Track.AppleId.Should().Be("apple-mr-brightside");
        content.Track.DurationMs.Should().Be(222000);
    }

    [Fact]
    public async Task Given_A_Lowercase_Isrc_When_The_Lookup_Endpoint_Is_Called_Then_It_Is_Normalized_And_The_Matching_Track_Is_Returned()
    {
        factory.CatalogLookup.Seed(ApiKnownTracks.MrBrightsideTrack());

        var response = await this.client.GetAsync("/lookup?isrc=usir20400274");
        var content = await response.Content.ReadFromJsonAsync<CatalogLookupResponseContract>();

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        content.Should().NotBeNull();
        content!.Track.Should().NotBeNull();
        content.Track!.Isrc.Should().Be("USIR20400274");
    }

    [Fact]
    public async Task Given_A_Known_Apple_Id_When_The_Lookup_Endpoint_Is_Called_Then_It_Returns_The_Matching_Track()
    {
        factory.CatalogLookup.Seed(ApiKnownTracks.MrBrightsideTrack());

        var response = await this.client.GetAsync("/lookup?appleId=apple-mr-brightside");
        var content = await response.Content.ReadFromJsonAsync<CatalogLookupResponseContract>();

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        content.Should().NotBeNull();
        content!.Track.Should().NotBeNull();
        content.Track!.Isrc.Should().Be("USIR20400274");
    }

    [Fact]
    public async Task Given_A_Known_Isrc_And_Matching_Duration_When_The_Lookup_Endpoint_Is_Called_Then_It_Returns_The_Matching_Track()
    {
        factory.CatalogLookup.Seed(ApiKnownTracks.MrBrightsideTrack());

        var response = await this.client.GetAsync("/lookup?isrc=USIR20400274&durationMs=222000");
        var content = await response.Content.ReadFromJsonAsync<CatalogLookupResponseContract>();

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        content.Should().NotBeNull();
        content!.Track.Should().NotBeNull();
        content.Track!.DurationMs.Should().Be(222000);
    }

    [Fact]
    public async Task Given_A_Known_Isrc_And_Non_Matching_Duration_When_The_Lookup_Endpoint_Is_Called_Then_It_Returns_Not_Found()
    {
        factory.CatalogLookup.Seed(ApiKnownTracks.MrBrightsideTrack());

        var response = await this.client.GetAsync("/lookup?isrc=USIR20400274&durationMs=111000");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Given_An_Unknown_Isrc_When_The_Lookup_Endpoint_Is_Called_Then_It_Returns_Not_Found()
    {
        factory.CatalogLookup.Clear();

        var response = await this.client.GetAsync("/lookup?isrc=UNKNOWN123456");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Given_No_Supported_Identifier_When_The_Lookup_Endpoint_Is_Called_Then_It_Returns_Bad_Request()
    {
        var response = await this.client.GetAsync("/lookup");
        var content = await response.Content.ReadFromJsonAsync<ErrorResponseContract>();

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        content.Should().NotBeNull();
        content!.Error.Should().Contain("identifier");
    }

    [Fact]
    public async Task Given_A_Negative_Duration_When_The_Lookup_Endpoint_Is_Called_Then_It_Returns_Bad_Request()
    {
        var response = await this.client.GetAsync("/lookup?isrc=USIR20400274&durationMs=-1");
        var content = await response.Content.ReadFromJsonAsync<ErrorResponseContract>();

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        content.Should().NotBeNull();
        content!.Error.Should().Contain("non-negative");
    }

    private sealed class CatalogLookupResponseContract
    {
        public LookupTrackContract? Track { get; set; }
    }

    private sealed class LookupTrackContract
    {
        public string Title { get; set; } = string.Empty;

        public string Artist { get; set; } = string.Empty;

        public string? Isrc { get; set; }

        public string? Mbid { get; set; }

        public string? AppleId { get; set; }

        public string? SpotifyId { get; set; }

        public int? DurationMs { get; set; }
    }

    private sealed class ErrorResponseContract
    {
        public string Error { get; set; } = string.Empty;
    }
}
