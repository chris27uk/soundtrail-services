using FluentAssertions;
using Soundtrail.Services.Tests.Unit.Api.Infrastructure;

namespace Soundtrail.Services.Tests.Unit.Api.Features.Search.UnknownQuery;

public sealed class QueueingTests
{
    [Fact]
    public async Task Given_An_Unknown_Query_When_Searching_Then_A_Lookup_Music_Request_Is_Enqueued()
    {
        var env = SearchMusicHandlerTestEnvironment.WithNoKnownTracks();

        await env.Handler.Handle(env.Request("rare unknown song"));

        env.EnqueueMusicRequests.Requests.Should().ContainSingle();
    }

    [Fact]
    public async Task Given_An_Unknown_Query_When_Searching_Then_Enqueued_Request_Query_Is_Normalized()
    {
        var env = SearchMusicHandlerTestEnvironment.WithNoKnownTracks();

        await env.Handler.Handle(env.Request("rare unknown song"));

        env.EnqueueMusicRequests.Requests[0].Query.Should().Be("rare unknown song");
    }

    [Fact]
    public async Task Given_An_Unknown_Query_When_Searching_Then_Enqueued_Request_TrustLevel_Defaults_To_Zero()
    {
        var env = SearchMusicHandlerTestEnvironment.WithNoKnownTracks();

        await env.Handler.Handle(env.Request("rare unknown song"));

        env.EnqueueMusicRequests.Requests[0].TrustLevel.Should().Be(0);
    }

    [Fact]
    public async Task Given_An_Unknown_Query_When_Searching_Then_Enqueued_Request_RiskScore_Defaults_To_Zero()
    {
        var env = SearchMusicHandlerTestEnvironment.WithNoKnownTracks();

        await env.Handler.Handle(env.Request("rare unknown song"));

        env.EnqueueMusicRequests.Requests[0].RiskScore.Should().Be(0);
    }

    [Fact]
    public async Task Given_An_Unknown_Query_When_Searching_Then_Enqueued_Request_OccurredAt_Is_Populated()
    {
        var env = SearchMusicHandlerTestEnvironment.WithNoKnownTracks();
        var before = DateTimeOffset.UtcNow;

        await env.Handler.Handle(env.Request("rare unknown song"));

        var after = DateTimeOffset.UtcNow;
        (env.EnqueueMusicRequests.Requests[0].OccurredAt >= before &&
         env.EnqueueMusicRequests.Requests[0].OccurredAt <= after)
            .Should().BeTrue();
    }

    [Fact]
    public async Task Given_An_Unknown_Query_When_Searching_Then_Enqueued_Request_CorrelationId_Is_Populated()
    {
        var env = SearchMusicHandlerTestEnvironment.WithNoKnownTracks();

        await env.Handler.Handle(env.Request("rare unknown song"));

        env.EnqueueMusicRequests.Requests[0].CorrelationId.Should().NotBeNullOrWhiteSpace();
    }
}
