using FluentAssertions;
using Soundtrail.Services.Features.Search;
using Soundtrail.Services.Features.Search.Models;
using Soundtrail.Services.Tests.Api.Unit.Infrastructure;

namespace Soundtrail.Services.Tests.Api.Unit.Features.Search.MinimumConfidenceSatisfied;

public sealed class ResponsesTests
{
    [Fact]
    public async Task Given_A_Result_That_Satisfies_Minimum_Confidence_When_Searching_Then_Response_Status_Is_Resolved()
    {
        var env = SearchMusicHandlerTestEnvironment.WithKnownTrack();

        var response = await env.Handler.Handle(env.Request("mr brightside", minConfidence: 0.90));

        response.Status.Should().Be(ResolutionStatus.Resolved);
    }

    [Fact]
    public async Task Given_A_Result_That_Satisfies_Minimum_Confidence_When_Searching_Then_Response_Contains_A_Single_Result()
    {
        var env = SearchMusicHandlerTestEnvironment.WithKnownTrack();

        var response = await env.Handler.Handle(env.Request("mr brightside", minConfidence: 0.90));

        response.Results.Should().ContainSingle();
    }

    [Fact]
    public async Task Given_A_Result_That_Satisfies_Minimum_Confidence_When_Searching_Then_Response_Source_Is_Local()
    {
        var env = SearchMusicHandlerTestEnvironment.WithKnownTrack();

        var response = await env.Handler.Handle(env.Request("mr brightside", minConfidence: 0.90));

        response.Source.Should().Be("local");
    }

    [Fact]
    public async Task Given_A_Result_That_Satisfies_Minimum_Confidence_When_Searching_Then_Response_Query_Matches_Request()
    {
        var env = SearchMusicHandlerTestEnvironment.WithKnownTrack();

        var response = await env.Handler.Handle(env.Request("mr brightside", minConfidence: 0.90));

        response.Query.Value.Should().Be("mr brightside");
    }

    [Fact]
    public async Task Given_A_Result_That_Satisfies_Minimum_Confidence_When_Searching_Then_Result_Title_Is_Preserved()
    {
        var env = SearchMusicHandlerTestEnvironment.WithKnownTrack();

        var response = await env.Handler.Handle(env.Request("mr brightside", minConfidence: 0.90));

        response.Results[0].Title.Value.Should().Be("Mr. Brightside");
    }

    [Fact]
    public async Task Given_A_Result_That_Satisfies_Minimum_Confidence_When_Searching_Then_Result_Artist_Is_Preserved()
    {
        var env = SearchMusicHandlerTestEnvironment.WithKnownTrack();

        var response = await env.Handler.Handle(env.Request("mr brightside", minConfidence: 0.90));

        response.Results[0].Artist.Value.Should().Be("The Killers");
    }

    [Fact]
    public async Task Given_A_Result_That_Satisfies_Minimum_Confidence_When_Searching_Then_Result_Isrc_Is_Preserved()
    {
        var env = SearchMusicHandlerTestEnvironment.WithKnownTrack();

        var response = await env.Handler.Handle(env.Request("mr brightside", minConfidence: 0.90));

        response.Results[0].Isrc?.Value.Should().Be("USIR20400274");
    }

    [Fact]
    public async Task Given_A_Result_That_Satisfies_Minimum_Confidence_When_Searching_Then_Result_Mbid_Is_Preserved()
    {
        var env = SearchMusicHandlerTestEnvironment.WithKnownTrack();

        var response = await env.Handler.Handle(env.Request("mr brightside", minConfidence: 0.90));

        response.Results[0].Mbid?.Value.Should().Be("mr-brightside-mbid");
    }

    [Fact]
    public async Task Given_A_Result_That_Satisfies_Minimum_Confidence_When_Searching_Then_Result_AppleId_Is_Preserved()
    {
        var env = SearchMusicHandlerTestEnvironment.WithKnownTrack();

        var response = await env.Handler.Handle(env.Request("mr brightside", minConfidence: 0.90));

        response.Results[0].AppleId?.Value.Should().Be("apple-mr-brightside");
    }

    [Fact]
    public async Task Given_A_Result_That_Satisfies_Minimum_Confidence_When_Searching_Then_Result_SpotifyId_Is_Preserved()
    {
        var env = SearchMusicHandlerTestEnvironment.WithKnownTrack();

        var response = await env.Handler.Handle(env.Request("mr brightside", minConfidence: 0.90));

        response.Results[0].SpotifyId?.Value.Should().Be("spotify-mr-brightside");
    }

    [Fact]
    public async Task Given_A_Result_That_Satisfies_Minimum_Confidence_When_Searching_Then_Result_Confidence_Is_Preserved()
    {
        var env = SearchMusicHandlerTestEnvironment.WithKnownTrack();

        var response = await env.Handler.Handle(env.Request("mr brightside", minConfidence: 0.90));

        response.Results[0].Confidence.Value.Should().Be(0.98);
    }
}
