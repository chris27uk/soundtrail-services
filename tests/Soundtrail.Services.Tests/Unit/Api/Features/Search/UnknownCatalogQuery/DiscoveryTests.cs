using FluentAssertions;
using Soundtrail.Services.Tests.Unit.Api.Infrastructure;

namespace Soundtrail.Services.Tests.Unit.Api.Features.Search.UnknownCatalogQuery;

public sealed class DiscoveryTests
{
    [Fact]
    public async Task Given_An_Unknown_Query_When_Searching_Then_A_Discovery_Request_Is_Queued()
    {
        var env = SearchCatalogHandlerTestEnvironment.WithNoKnownResults();

        var response = await env.Handler.Handle(env.Request("rare unknown song"));

        env.RequestDiscovery.Requests.Should().ContainSingle();
        env.EnqueueMusicRequests.Requests.Should().ContainSingle();
        response.Discovery.WillBeLookedUp.Should().BeTrue();
        response.Discovery.Reason.Should().Be("Local results incomplete");
    }

    [Fact]
    public async Task Given_An_Existing_Discovery_Status_When_Searching_Then_No_Duplicate_Request_Is_Queued()
    {
        var env = SearchCatalogHandlerTestEnvironment.WithPendingDiscovery();

        var response = await env.Handler.Handle(env.Request("rare unknown song"));

        env.EnqueueMusicRequests.Requests.Should().BeEmpty();
        response.Discovery.WillBeLookedUp.Should().BeTrue();
        response.Discovery.RetryAfterSeconds.Should().Be(30);
    }

    [Fact]
    public async Task Given_A_Previously_Recorded_Discovery_Request_When_Searching_Then_A_Duplicate_Request_Is_Not_Queued()
    {
        var env = SearchCatalogHandlerTestEnvironment.WithRecordedDiscoveryRequest();

        var response = await env.Handler.Handle(env.Request("rare unknown song"));

        env.RequestDiscovery.Requests.Should().BeEmpty();
        env.EnqueueMusicRequests.Requests.Should().BeEmpty();
        response.Discovery.WillBeLookedUp.Should().BeTrue();
        response.Discovery.Reason.Should().Be("Local results incomplete");
    }
}
