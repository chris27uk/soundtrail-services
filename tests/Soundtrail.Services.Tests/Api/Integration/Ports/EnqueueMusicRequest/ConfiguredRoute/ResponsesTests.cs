using FluentAssertions;

namespace Soundtrail.Services.Tests.Api.Integration.Ports.EnqueueMusicRequest.Contract;

public sealed partial class EnqueueMusicRequestPortContractTests
{
    public static IEnumerable<object[]> Modes =>
    [
        [EnqueueMusicRequestPortMode.InMemoryFake],
        [EnqueueMusicRequestPortMode.WolverineLocal]
    ];

    [Theory]
    [MemberData(nameof(Modes))]
    public async Task Given_A_Configured_Route_When_A_Request_Is_Enqueued_Then_The_Message_Is_Sent(EnqueueMusicRequestPortMode mode)
    {
        await using var env = await EnqueueMusicRequestTestEnvironment.CreateAsync(mode, configuredRoute: true);
        var request = EnqueueMusicRequestTestEnvironment.Request("mr brightside");

        await env.EnqueueMusicRequest.EnqueueAsync(request, CancellationToken.None);
        var actual = await env.WaitForCapturedRequestAsync(TimeSpan.FromSeconds(5));

        actual.Should().BeEquivalentTo(request);
    }
}
