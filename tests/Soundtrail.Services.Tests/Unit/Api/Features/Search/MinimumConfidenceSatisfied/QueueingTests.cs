using FluentAssertions;
using Soundtrail.Services.Tests.Unit.Api.Infrastructure;

namespace Soundtrail.Services.Tests.Unit.Api.Features.Search.MinimumConfidenceSatisfied;

public sealed class QueueingTests
{
    [Fact]
    public async Task Given_A_Result_That_Satisfies_Minimum_Confidence_When_Searching_Then_No_Lookup_Music_Request_Is_Enqueued()
    {
        var env = SearchMusicHandlerTestEnvironment.WithKnownTrack();

        await env.Handler.Handle(env.Request("mr brightside", minConfidence: 0.90));

        env.EnqueueMusicRequests.Requests.Should().BeEmpty();
    }
}
