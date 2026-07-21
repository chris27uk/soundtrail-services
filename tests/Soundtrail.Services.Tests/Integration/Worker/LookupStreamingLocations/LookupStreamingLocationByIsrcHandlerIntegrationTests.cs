using Soundtrail.Domain.Discovery;
using Soundtrail.Domain.Discovery.Messages;

namespace Soundtrail.Services.Tests.Integration.Worker.LookupStreamingLocations;

public sealed class LookupStreamingLocationByIsrcHandlerIntegrationTests
{
    [Fact]
    public async Task Given_A_Raven_Track_And_A_WireMock_Odesli_Response_When_Handling_Then_A_Succeeded_Result_Is_Published()
    {
        using var environment = await LookupStreamingLocationHandlerIntegrationTestEnvironment.CreateAsync(
            """{"linksByPlatform":{"spotify":{"url":"https://open.spotify.com/track/integration-1901"}}}""");
        var subject = environment.CreateIsrcSubject();

        await subject.Handle(environment.CreateIsrcRequest(), CancellationToken.None);

        var completed = environment.CommandBus.Messages.Single().Should().BeOfType<CatalogLookupCompleted>().Subject;
        completed.Result.Should().BeOfType<LookupResult.Succeeded>();
    }

    [Fact]
    public async Task Given_No_Provider_Link_When_Handling_Then_A_Not_Found_Result_Is_Published()
    {
        using var environment = await LookupStreamingLocationHandlerIntegrationTestEnvironment.CreateAsync(
            """{"linksByPlatform":{"appleMusic":{"url":"https://music.apple.com/track/integration-1902"}}}""");
        var subject = environment.CreateIsrcSubject();

        await subject.Handle(environment.CreateIsrcRequest(), CancellationToken.None);

        var result = environment.CommandBus.Messages.Single()
            .Should().BeOfType<CatalogLookupCompleted>().Subject.Result
            .Should().BeOfType<LookupResult.NotFound>().Subject;
        result.Reason.Should().Be("Streaming location was not found for the requested provider.");
    }
}
