using FluentAssertions;
using Soundtrail.Services.Tests.Api.Unit.Infrastructure;

namespace Soundtrail.Services.Tests.Api.Unit.Features.Search.LimitedKnownTracks;

public sealed class QueueingTests
{
    [Fact]
    public async Task Given_Multiple_Known_Local_Tracks_When_Searching_With_A_Limit_Of_One_Then_No_Lookup_Music_Request_Is_Enqueued()
    {
        var env = SearchMusicHandlerTestEnvironment.WithMultipleKnownTracks();

        await env.Handler.Handle(env.Request("the killers", limit: 1));

        env.EnqueueMusicRequests.Requests.Should().BeEmpty();
    }
}
