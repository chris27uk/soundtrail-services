using FluentAssertions;
using Soundtrail.Services.Features.Search;
using Soundtrail.Services.Features.Search.Models;
using Soundtrail.Services.Tests.Api.Unit.Infrastructure;

namespace Soundtrail.Services.Tests.Api.Unit.Features.Search.LimitedKnownTracks;

public sealed class ResponsesTests
{
    [Fact]
    public async Task Given_Multiple_Known_Local_Tracks_When_Searching_With_A_Limit_Of_One_Then_Response_Status_Is_Resolved()
    {
        var env = SearchMusicHandlerTestEnvironment.WithMultipleKnownTracks();

        var response = await env.Handler.Handle(env.Request("the killers", limit: 1));

        response.Status.Should().Be(ResolutionStatus.Resolved);
    }

    [Fact]
    public async Task Given_Multiple_Known_Local_Tracks_When_Searching_With_A_Limit_Of_One_Then_Response_Contains_A_Single_Result()
    {
        var env = SearchMusicHandlerTestEnvironment.WithMultipleKnownTracks();

        var response = await env.Handler.Handle(env.Request("the killers", limit: 1));

        response.Results.Should().ContainSingle();
    }

    [Fact]
    public async Task Given_Multiple_Known_Local_Tracks_When_Searching_With_A_Limit_Of_One_Then_First_Result_Title_Is_Returned()
    {
        var env = SearchMusicHandlerTestEnvironment.WithMultipleKnownTracks();

        var response = await env.Handler.Handle(env.Request("the killers", limit: 1));

        response.Results[0].Title.Value.Should().Be("Mr. Brightside");
    }
}
