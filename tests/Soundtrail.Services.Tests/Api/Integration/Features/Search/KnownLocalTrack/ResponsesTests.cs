using FluentAssertions;
using Soundtrail.Services.Features.Search;
using Soundtrail.Services.Features.Search.Models;
using System.Net;
using System.Net.Http.Json;

namespace Soundtrail.Services.Tests.Api.Integration.Features.Search.KnownLocalTrack;

public sealed class ResponsesTests(SoundtrailServicesApiFactory factory) : IClassFixture<SoundtrailServicesApiFactory>
{
    private readonly HttpClient client = factory.CreateClient();

    [Fact]
    public async Task Given_A_Known_Local_Track_When_Searching_Then_Http_Status_Code_Is_Ok()
    {
        factory.SearchMusicHandler.ClearRequests();
        factory.SearchMusicHandler.RespondWith(
            SearchMusicResponse.Resolved(
                SearchQuery.From("mr brightside"),
                [ApiKnownTracks.MrBrightside()]));

        var response = await client.GetAsync("/search?q=mr%20brightside");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Given_A_Known_Local_Track_When_Searching_Then_Response_Status_Is_Resolved()
    {
        factory.SearchMusicHandler.ClearRequests();
        factory.SearchMusicHandler.RespondWith(
            SearchMusicResponse.Resolved(
                SearchQuery.From("mr brightside"),
                [ApiKnownTracks.MrBrightside()]));

        var response = await client.GetFromJsonAsync<SearchResponseContract>("/search?q=mr%20brightside");

        response!.Status.Should().Be("resolved");
    }

    [Fact]
    public async Task Given_A_Known_Local_Track_When_Searching_Then_Response_Query_Matches_Request()
    {
        factory.SearchMusicHandler.ClearRequests();
        factory.SearchMusicHandler.RespondWith(
            SearchMusicResponse.Resolved(
                SearchQuery.From("mr brightside"),
                [ApiKnownTracks.MrBrightside()]));

        var response = await client.GetFromJsonAsync<SearchResponseContract>("/search?q=mr%20brightside");

        response!.Query.Should().Be("mr brightside");
    }

    [Fact]
    public async Task Given_A_Known_Local_Track_When_Searching_Then_Response_Contains_A_Single_Result()
    {
        factory.SearchMusicHandler.ClearRequests();
        factory.SearchMusicHandler.RespondWith(
            SearchMusicResponse.Resolved(
                SearchQuery.From("mr brightside"),
                [ApiKnownTracks.MrBrightside()]));

        var response = await client.GetFromJsonAsync<SearchResponseContract>("/search?q=mr%20brightside");

        response!.Results.Should().ContainSingle();
    }

    [Fact]
    public async Task Given_A_Known_Local_Track_When_Searching_Then_Result_Title_Is_Returned()
    {
        factory.SearchMusicHandler.ClearRequests();
        factory.SearchMusicHandler.RespondWith(
            SearchMusicResponse.Resolved(
                SearchQuery.From("mr brightside"),
                [ApiKnownTracks.MrBrightside()]));

        var response = await client.GetFromJsonAsync<SearchResponseContract>("/search?q=mr%20brightside");

        response!.Results[0].Title.Should().Be("Mr. Brightside");
    }

    [Fact]
    public async Task Given_A_Known_Local_Track_When_Searching_Then_Result_Artist_Is_Returned()
    {
        factory.SearchMusicHandler.ClearRequests();
        factory.SearchMusicHandler.RespondWith(
            SearchMusicResponse.Resolved(
                SearchQuery.From("mr brightside"),
                [ApiKnownTracks.MrBrightside()]));

        var response = await client.GetFromJsonAsync<SearchResponseContract>("/search?q=mr%20brightside");

        response!.Results[0].Artist.Should().Be("The Killers");
    }

    [Fact]
    public async Task Given_A_Known_Local_Track_When_Searching_Then_Result_Isrc_Is_Returned()
    {
        factory.SearchMusicHandler.ClearRequests();
        factory.SearchMusicHandler.RespondWith(
            SearchMusicResponse.Resolved(
                SearchQuery.From("mr brightside"),
                [ApiKnownTracks.MrBrightside()]));

        var response = await client.GetFromJsonAsync<SearchResponseContract>("/search?q=mr%20brightside");

        response!.Results[0].Isrc.Should().Be("USIR20400274");
    }

    [Fact]
    public async Task Given_A_Known_Local_Track_When_Searching_Then_Result_Mbid_Is_Returned()
    {
        factory.SearchMusicHandler.ClearRequests();
        factory.SearchMusicHandler.RespondWith(
            SearchMusicResponse.Resolved(
                SearchQuery.From("mr brightside"),
                [ApiKnownTracks.MrBrightside()]));

        var response = await client.GetFromJsonAsync<SearchResponseContract>("/search?q=mr%20brightside");

        response!.Results[0].Mbid.Should().Be("mr-brightside-mbid");
    }

    [Fact]
    public async Task Given_A_Known_Local_Track_When_Searching_Then_Result_AppleId_Is_Returned()
    {
        factory.SearchMusicHandler.ClearRequests();
        factory.SearchMusicHandler.RespondWith(
            SearchMusicResponse.Resolved(
                SearchQuery.From("mr brightside"),
                [ApiKnownTracks.MrBrightside()]));

        var response = await client.GetFromJsonAsync<SearchResponseContract>("/search?q=mr%20brightside");

        response!.Results[0].AppleId.Should().Be("apple-mr-brightside");
    }

    [Fact]
    public async Task Given_A_Known_Local_Track_When_Searching_Then_Result_SpotifyId_Is_Returned()
    {
        factory.SearchMusicHandler.ClearRequests();
        factory.SearchMusicHandler.RespondWith(
            SearchMusicResponse.Resolved(
                SearchQuery.From("mr brightside"),
                [ApiKnownTracks.MrBrightside()]));

        var response = await client.GetFromJsonAsync<SearchResponseContract>("/search?q=mr%20brightside");

        response!.Results[0].SpotifyId.Should().Be("spotify-mr-brightside");
    }

    [Fact]
    public async Task Given_A_Known_Local_Track_When_Searching_Then_Result_Confidence_Is_Returned()
    {
        factory.SearchMusicHandler.ClearRequests();
        factory.SearchMusicHandler.RespondWith(
            SearchMusicResponse.Resolved(
                SearchQuery.From("mr brightside"),
                [ApiKnownTracks.MrBrightside()]));

        var response = await client.GetFromJsonAsync<SearchResponseContract>("/search?q=mr%20brightside");

        response!.Results[0].Confidence.Should().Be(0.98);
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

        public string Artist { get; set; } = string.Empty;

        public string? Isrc { get; set; }

        public string? Mbid { get; set; }

        public string? AppleId { get; set; }

        public string? SpotifyId { get; set; }

        public double Confidence { get; set; }
    }
}
