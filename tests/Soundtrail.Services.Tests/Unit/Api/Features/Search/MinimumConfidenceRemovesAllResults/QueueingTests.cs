using FluentAssertions;
using Soundtrail.Services.Tests.Unit.Api.Infrastructure;

namespace Soundtrail.Services.Tests.Unit.Api.Features.Search.MinimumConfidenceRemovesAllResults;

public sealed class QueueingTests
{
    [Fact]
    public async Task Given_Results_Below_Minimum_Confidence_When_Searching_Then_A_Lookup_Music_Request_Is_Enqueued()
    {
        var env = SearchMusicHandlerTestEnvironment.WithKnownTrack();

        await env.Handler.Handle(env.Request("mr brightside", minConfidence: 0.99));

        env.EnqueueMusicRequests.Requests.Should().ContainSingle();
    }
}
