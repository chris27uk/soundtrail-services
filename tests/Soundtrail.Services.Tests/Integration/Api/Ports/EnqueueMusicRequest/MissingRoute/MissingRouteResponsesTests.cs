using FluentAssertions;
using Soundtrail.Services.Tests.Integration.Api.Ports.EnqueueMusicRequest.ConfiguredRoute;

namespace Soundtrail.Services.Tests.Integration.Api.Ports.EnqueueMusicRequest.MissingRoute;

public sealed class MissingRouteResponsesTests
{
    [Theory]
    [MemberData(nameof(EnqueueMusicRequestPortContractModes.All), MemberType = typeof(EnqueueMusicRequestPortContractModes))]
    public async Task Given_No_Configured_Route_When_A_Request_Is_Enqueued_Then_An_Exception_Is_Thrown(EnqueueMusicRequestPortMode mode)
    {
        await using var env = await EnqueueMusicRequestTestEnvironment.CreateAsync(mode, configuredRoute: false);
        var request = EnqueueMusicRequestTestEnvironment.Request("mr brightside");

        Func<Task> act = () => env.EnqueueMusicRequest.EnqueueAsync(request, CancellationToken.None);

        await act.Should().ThrowAsync<Exception>();
    }
}
