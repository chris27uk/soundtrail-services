using FluentAssertions;
using Soundtrail.Services.Api.Features.Search.TrackSearch;
using Soundtrail.Services.Tests.Unit.Api.Infrastructure;

namespace Soundtrail.Services.Tests.Unit.Api.Features.Search.UnknownQuery;

public sealed class ResponsesTests
{
    [Fact]
    public async Task Given_An_Unknown_Query_When_Searching_Then_Response_Status_Is_Pending()
    {
        var env = SearchMusicHandlerTestEnvironment.WithNoKnownTracks();

        var response = await env.Handler.Handle(env.Request("rare unknown song"));

        response.Status.Should().Be(ResolutionStatus.Pending);
    }

    [Fact]
    public async Task Given_An_Unknown_Query_When_Searching_Then_Response_Source_Is_Local()
    {
        var env = SearchMusicHandlerTestEnvironment.WithNoKnownTracks();

        var response = await env.Handler.Handle(env.Request("rare unknown song"));

        response.Source.Should().Be("local");
    }

    [Fact]
    public async Task Given_An_Unknown_Query_When_Searching_Then_Response_Query_Matches_Request()
    {
        var env = SearchMusicHandlerTestEnvironment.WithNoKnownTracks();

        var response = await env.Handler.Handle(env.Request("rare unknown song"));

        response.Query.Should().Be("rare unknown song");
    }

    [Fact]
    public async Task Given_An_Unknown_Query_When_Searching_Then_Response_RetryAfterSeconds_Is_Defaulted()
    {
        var env = SearchMusicHandlerTestEnvironment.WithNoKnownTracks();

        var response = await env.Handler.Handle(env.Request("rare unknown song"));

        response.RetryAfterSeconds.Should().Be(60);
    }

    [Fact]
    public async Task Given_An_Unknown_Query_When_Searching_Then_Response_Results_Are_Empty()
    {
        var env = SearchMusicHandlerTestEnvironment.WithNoKnownTracks();

        var response = await env.Handler.Handle(env.Request("rare unknown song"));

        response.Results.Should().BeEmpty();
    }
}
