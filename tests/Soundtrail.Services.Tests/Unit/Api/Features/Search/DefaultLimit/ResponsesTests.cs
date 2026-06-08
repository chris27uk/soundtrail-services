using FluentAssertions;
using Soundtrail.Services.Api.Features.Search.TrackSearch;
using Soundtrail.Services.Tests.Unit.Api.Infrastructure;

namespace Soundtrail.Services.Tests.Unit.Api.Features.Search.DefaultLimit;

public sealed class ResponsesTests
{
    [Fact]
    public async Task Given_Multiple_Known_Local_Tracks_When_Searching_Without_A_Limit_Then_The_Default_Limit_Does_Not_Trim_The_Results()
    {
        var env = SearchMusicHandlerTestEnvironment.WithMultipleKnownTracks();

        var response = await env.Handler.Handle(env.Request("the killers"));

        response.Status.Should().Be(ResolutionStatus.Resolved);
        response.Results.Should().HaveCount(2);
    }
}
