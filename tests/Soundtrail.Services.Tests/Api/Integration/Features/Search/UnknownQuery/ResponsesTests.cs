using FluentAssertions;
using Soundtrail.Services.Features.Search;
using Soundtrail.Services.Features.Search.Models;
using System.Net;
using System.Net.Http.Json;

namespace Soundtrail.Services.Tests.Api.Integration.Features.Search.UnknownQuery;

public sealed class ResponsesTests(SoundtrailServicesApiFactory factory) : IClassFixture<SoundtrailServicesApiFactory>
{
    private readonly HttpClient client = factory.CreateClient();

    [Fact]
    public async Task Given_An_Unknown_Query_When_Searching_Then_Http_Status_Code_Is_Ok()
    {
        factory.SearchMusicHandler.ClearRequests();
        factory.SearchMusicHandler.RespondWith(SearchMusicResponse.Pending(SearchQuery.From("rare unknown song")));

        var response = await client.GetAsync("/search?q=rare%20unknown%20song");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Given_An_Unknown_Query_When_Searching_Then_Response_Status_Is_Pending()
    {
        factory.SearchMusicHandler.ClearRequests();
        factory.SearchMusicHandler.RespondWith(SearchMusicResponse.Pending(SearchQuery.From("rare unknown song")));

        var response = await client.GetFromJsonAsync<SearchResponseContract>("/search?q=rare%20unknown%20song");

        response!.Status.Should().Be("pending");
    }

    [Fact]
    public async Task Given_An_Unknown_Query_When_Searching_Then_Response_Query_Matches_Request()
    {
        factory.SearchMusicHandler.ClearRequests();
        factory.SearchMusicHandler.RespondWith(SearchMusicResponse.Pending(SearchQuery.From("rare unknown song")));

        var response = await client.GetFromJsonAsync<SearchResponseContract>("/search?q=rare%20unknown%20song");

        response!.Query.Should().Be("rare unknown song");
    }

    [Fact]
    public async Task Given_An_Unknown_Query_When_Searching_Then_Response_RetryAfterSeconds_Is_Defaulted()
    {
        factory.SearchMusicHandler.ClearRequests();
        factory.SearchMusicHandler.RespondWith(SearchMusicResponse.Pending(SearchQuery.From("rare unknown song")));

        var response = await client.GetFromJsonAsync<SearchResponseContract>("/search?q=rare%20unknown%20song");

        response!.RetryAfterSeconds.Should().Be(60);
    }

    [Fact]
    public async Task Given_An_Unknown_Query_When_Searching_Then_Response_Results_Are_Empty()
    {
        factory.SearchMusicHandler.ClearRequests();
        factory.SearchMusicHandler.RespondWith(SearchMusicResponse.Pending(SearchQuery.From("rare unknown song")));

        var response = await client.GetFromJsonAsync<SearchResponseContract>("/search?q=rare%20unknown%20song");

        response!.Results.Should().BeEmpty();
    }

    private sealed class SearchResponseContract
    {
        public string Status { get; set; } = string.Empty;

        public string Query { get; set; } = string.Empty;

        public int? RetryAfterSeconds { get; set; }

        public List<object> Results { get; set; } = [];
    }
}
