using Soundtrail.Domain.Discovery;
using Soundtrail.Domain.Discovery.Messages;

namespace Soundtrail.Services.Tests.Integration.Worker.LookupStreamingLocations;

public sealed class LookupStreamingLocationByTrackMetadataHandlerIntegrationTests
{
    [Fact]
    public async Task Given_A_Raven_Track_And_A_WireMock_Odesli_Response_When_Handling_Then_A_Succeeded_Result_Is_Published()
    {
        using var environment = await LookupStreamingLocationHandlerIntegrationTestEnvironment.CreateAsync(
            """{"linksByPlatform":{"appleMusic":{"url":"https://music.apple.com/track/integration-1903"}}}""");
        var subject = environment.CreateMetadataSubject();

        await subject.Handle(environment.CreateMetadataRequest(), CancellationToken.None);

        var completed = environment.CommandBus.Messages.Single().Should().BeOfType<CatalogLookupCompleted>().Subject;
        completed.Result.Should().BeOfType<LookupResult.Succeeded>();
    }
}
