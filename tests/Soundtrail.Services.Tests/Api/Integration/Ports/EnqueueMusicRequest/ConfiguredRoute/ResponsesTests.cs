using FluentAssertions;

namespace Soundtrail.Services.Tests.Api.Integration.Ports.EnqueueMusicRequest.WolverineLocal.ConfiguredRoute;

public sealed class WolverineLocalPortResponsesTests
{
    [Fact]
    public async Task Given_A_Configured_Wolverine_Route_When_A_Request_Is_Enqueued_Then_The_Message_Is_Sent()
    {
        await using var env = await EnqueueMusicRequestTestEnvironment.WithConfiguredRouteAsync();
        var request = EnqueueMusicRequestTestEnvironment.Request("mr brightside");

        await env.EnqueueMusicRequest.EnqueueAsync(request, CancellationToken.None);
        var actual = await env.WaitForCapturedRequestAsync(TimeSpan.FromSeconds(5));

        actual.Should().BeEquivalentTo(request);
    }
}
