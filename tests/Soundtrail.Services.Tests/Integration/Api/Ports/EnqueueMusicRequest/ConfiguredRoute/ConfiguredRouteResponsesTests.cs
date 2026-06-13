using FluentAssertions;
using Soundtrail.Contracts.IntegrationMessaging.Commands;

namespace Soundtrail.Services.Tests.Integration.Api.Ports.EnqueueMusicRequest.ConfiguredRoute;

public sealed class ConfiguredRouteResponsesTests
{
    [Theory]
    [MemberData(nameof(EnqueueMusicRequestPortContractModes.All), MemberType = typeof(EnqueueMusicRequestPortContractModes))]
    public async Task Given_A_Configured_Route_When_A_Request_Is_Enqueued_Then_The_Message_Is_Sent(EnqueueMusicRequestPortMode mode)
    {
        await using var env = await EnqueueMusicRequestTestEnvironment.CreateAsync(mode, configuredRoute: true);
        var request = EnqueueMusicRequestTestEnvironment.Request("mr brightside");

        await env.EnqueueMusicRequest.EnqueueAsync(request, CancellationToken.None);
        if (mode == EnqueueMusicRequestPortMode.WolverineLocal)
        {
            return;
        }

        var actual = (LookupMusicRequestDto)await env.WaitForCapturedRequestAsync(TimeSpan.FromSeconds(5));

        actual.Query.Should().Be(request.Query.Value);
        actual.TrustLevel.Should().Be(request.TrustLevel);
        actual.RiskScore.Should().Be(request.RiskScore);
        actual.OccurredAt.Should().Be(request.OccurredAt);
        actual.CorrelationId.Should().Be(request.CorrelationId.Value);
    }
}
