using FluentAssertions;
using Soundtrail.Services.Api.Features.SearchMusic.TrackSearch;
using Soundtrail.Services.Tests.Unit.Api.Infrastructure;

namespace Soundtrail.Services.Tests.Unit.Api.Features.Search.MinimumConfidenceRemovesAllResults;

public sealed class ResponsesTests
{
    [Fact]
    public async Task Given_Results_Below_Minimum_Confidence_When_Searching_Then_Response_Status_Is_Pending()
    {
        var env = SearchMusicHandlerTestEnvironment.WithKnownTrack();

        var response = await env.Handler.Handle(env.Request("mr brightside", minConfidence: 0.99));

        response.Status.Should().Be(ResolutionStatus.Pending);
    }

    [Fact]
    public async Task Given_Results_Below_Minimum_Confidence_When_Searching_Then_Response_RetryAfterSeconds_Is_Defaulted()
    {
        var env = SearchMusicHandlerTestEnvironment.WithKnownTrack();

        var response = await env.Handler.Handle(env.Request("mr brightside", minConfidence: 0.99));

        response.RetryAfterSeconds.Should().Be(60);
    }

    [Fact]
    public async Task Given_Results_Below_Minimum_Confidence_When_Searching_Then_Response_Source_Is_Local()
    {
        var env = SearchMusicHandlerTestEnvironment.WithKnownTrack();

        var response = await env.Handler.Handle(env.Request("mr brightside", minConfidence: 0.99));

        response.Source.Should().Be("local");
    }

    [Fact]
    public async Task Given_Results_Below_Minimum_Confidence_When_Searching_Then_Response_Query_Matches_Request()
    {
        var env = SearchMusicHandlerTestEnvironment.WithKnownTrack();

        var response = await env.Handler.Handle(env.Request("mr brightside", minConfidence: 0.99));

        response.Query.Should().Be("mr brightside");
    }

    [Fact]
    public async Task Given_Results_Below_Minimum_Confidence_When_Searching_Then_Response_Results_Are_Empty()
    {
        var env = SearchMusicHandlerTestEnvironment.WithKnownTrack();

        var response = await env.Handler.Handle(env.Request("mr brightside", minConfidence: 0.99));

        response.Results.Should().BeEmpty();
    }
}
