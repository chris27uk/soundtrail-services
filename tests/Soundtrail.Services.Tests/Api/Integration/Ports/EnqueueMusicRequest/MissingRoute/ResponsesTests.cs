using FluentAssertions;

namespace Soundtrail.Services.Tests.Api.Integration.Ports.EnqueueMusicRequest.Contract;

public sealed partial class EnqueueMusicRequestPortContractTests
{
    [Theory]
    [MemberData(nameof(Modes))]
    public async Task Given_No_Configured_Route_When_A_Request_Is_Enqueued_Then_An_Exception_Is_Thrown(EnqueueMusicRequestPortMode mode)
    {
        await using var env = await EnqueueMusicRequestTestEnvironment.CreateAsync(mode, configuredRoute: false);
        var request = EnqueueMusicRequestTestEnvironment.Request("mr brightside");

        Func<Task> act = () => env.EnqueueMusicRequest.EnqueueAsync(request, CancellationToken.None);

        await act.Should().ThrowAsync<Exception>();
    }
}
