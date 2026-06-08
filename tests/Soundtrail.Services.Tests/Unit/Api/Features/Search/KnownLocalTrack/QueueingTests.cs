using FluentAssertions;
using Soundtrail.Services.Tests.Unit.Api.Infrastructure;

namespace Soundtrail.Services.Tests.Unit.Api.Features.Search.KnownLocalTrack;

public sealed class QueueingTests
{
    [Fact]
    public async Task Given_A_Known_Local_Track_When_Searching_Then_No_Lookup_Music_Request_Is_Enqueued()
    {
        var env = SearchMusicHandlerTestEnvironment.WithKnownTrack();

        await env.Handler.Handle(env.Request("mr brightside"));

        env.EnqueueMusicRequests.Requests.Should().BeEmpty();
    }
}
