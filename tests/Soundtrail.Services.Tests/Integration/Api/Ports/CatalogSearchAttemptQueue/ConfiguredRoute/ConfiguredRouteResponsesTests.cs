using FluentAssertions;
using Soundtrail.Contracts.IntegrationMessaging.Commands;

namespace Soundtrail.Services.Tests.Integration.Api.Ports.CatalogSearchAttemptQueue.ConfiguredRoute;

public sealed class ConfiguredRouteResponsesTests
{
    [Theory]
    [MemberData(nameof(CatalogSearchAttemptQueuePortContractModes.All), MemberType = typeof(CatalogSearchAttemptQueuePortContractModes))]
    public async Task Given_A_Configured_Route_When_A_Request_Is_Enqueued_Then_The_Message_Is_Sent(CatalogSearchAttemptQueuePortMode mode)
    {
        await using var env = await CatalogSearchAttemptQueueTestEnvironment.CreateAsync(mode, configuredRoute: true);
        var request = CatalogSearchAttemptQueueTestEnvironment.Request("mr brightside");

        await env.CatalogSearchAttemptQueue.EnqueueAsync(request, CancellationToken.None);
        if (mode == CatalogSearchAttemptQueuePortMode.WolverineLocal)
        {
            return;
        }

        var actual = (CatalogSearchAttemptDto)await env.WaitForCapturedRequestAsync(TimeSpan.FromSeconds(5));

        actual.Query.Should().Be(request.SearchCriteria.Query);
        actual.TrustLevel.Should().Be(request.TrustLevel);
        actual.RiskScore.Should().Be(request.RiskScore);
        actual.OccurredAt.Should().Be(request.OccurredAt);
        actual.CorrelationId.Should().Be(request.CorrelationId.Value);
    }
}
