using FluentAssertions;
using Soundtrail.Services.Api.Features.SearchMusic.TrackSearch;
using Soundtrail.Services.Tests.Unit.Api.Infrastructure;

namespace Soundtrail.Services.Tests.Unit.Api.Features.Search.NormalizedQuery;

public sealed class ResponsesTests
{
    [Fact]
    public async Task Given_A_Known_Local_Track_With_A_Query_That_Needs_Normalization_When_Searching_Then_The_Response_Is_Resolved()
    {
        var env = SearchMusicHandlerTestEnvironment.WithKnownTrack();

        var response = await env.Handler.Handle(env.Request("  MR.   BRIGHTSIDE!!! "));

        response.Status.Should().Be(ResolutionStatus.Resolved);
    }

    [Fact]
    public async Task Given_A_Known_Local_Track_With_A_Query_That_Needs_Normalization_When_Searching_Then_The_Response_Contains_A_Single_Result()
    {
        var env = SearchMusicHandlerTestEnvironment.WithKnownTrack();

        var response = await env.Handler.Handle(env.Request("  MR.   BRIGHTSIDE!!! "));

        response.Results.Should().ContainSingle();
    }
}
