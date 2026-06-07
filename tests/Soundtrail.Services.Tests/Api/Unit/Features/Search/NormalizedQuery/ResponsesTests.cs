using FluentAssertions;
using Soundtrail.Services.Api.Features.Search.TrackSearch;
using Soundtrail.Services.Tests.Api.Unit.Infrastructure;

namespace Soundtrail.Services.Tests.Api.Unit.Features.Search.NormalizedQuery;

public sealed class ResponsesTests
{
    [Fact]
    public async Task Given_A_Known_Local_Track_With_A_Query_That_Needs_Normalization_When_Searching_Then_The_Response_Is_Resolved()
    {
        var env = SearchMusicHandlerTestEnvironment.WithKnownTrack();

        var response = await env.Handler.Handle(env.Request("  MR.   BRIGHTSIDE!!! "));

        response.Status.Should().Be(ResolutionStatus.Resolved);
        response.Results.Should().ContainSingle();
    }
}
