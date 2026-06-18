using FluentAssertions;
using Soundtrail.Services.Tests.Integration.Api.Ports.CatalogSearchAttemptQueue.ConfiguredRoute;

namespace Soundtrail.Services.Tests.Integration.Api.Ports.CatalogSearchAttemptQueue.MissingRoute;

public sealed class MissingRouteResponsesTests
{
    [Theory]
    [MemberData(nameof(CatalogSearchAttemptQueuePortContractModes.All), MemberType = typeof(CatalogSearchAttemptQueuePortContractModes))]
    public async Task Given_No_Configured_Route_When_A_Request_Is_Enqueued_Then_An_Exception_Is_Thrown(CatalogSearchAttemptQueuePortMode mode)
    {
        await using var env = await CatalogSearchAttemptQueueTestEnvironment.CreateAsync(mode, configuredRoute: false);
        var request = CatalogSearchAttemptQueueTestEnvironment.Request("mr brightside");

        Func<Task> act = () => env.CatalogSearchAttemptQueue.EnqueueAsync(request, CancellationToken.None);

        await act.Should().ThrowAsync<Exception>();
    }
}
