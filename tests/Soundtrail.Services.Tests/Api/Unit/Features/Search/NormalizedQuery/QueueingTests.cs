using FluentAssertions;
using Soundtrail.Services.Tests.Api.Unit.Infrastructure;

namespace Soundtrail.Services.Tests.Api.Unit.Features.Search.NormalizedQuery;

public sealed class QueueingTests
{
    [Fact]
    public async Task Given_An_Unknown_Query_With_Case_Punctuation_And_Repeated_Whitespace_When_Searching_Then_The_Enqueued_Query_Is_Normalized()
    {
        var env = SearchMusicHandlerTestEnvironment.WithNoKnownTracks();

        await env.Handler.Handle(env.Request("  Rare!!!   UNKNOWN... Song  "));

        env.EnqueueMusicRequests.Requests[0].Query.Should().Be("rare unknown song");
    }
}
