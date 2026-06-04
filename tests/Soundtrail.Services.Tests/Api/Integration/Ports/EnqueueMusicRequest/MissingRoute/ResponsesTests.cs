using FluentAssertions;

namespace Soundtrail.Services.Tests.Api.Integration.Ports.EnqueueMusicRequest.WolverineLocal.MissingRoute;

public sealed class WolverineLocalPortResponsesTests
{
    [Fact]
    public async Task Given_No_Configured_Wolverine_Route_When_A_Request_Is_Enqueued_Then_An_Exception_Is_Thrown()
    {
        await using var env = await EnqueueMusicRequestTestEnvironment.WithoutConfiguredRouteAsync();
        var request = EnqueueMusicRequestTestEnvironment.Request("mr brightside");

        Func<Task> act = () => env.EnqueueMusicRequest.EnqueueAsync(request, CancellationToken.None);

        await act.Should().ThrowAsync<Exception>();
    }
}
