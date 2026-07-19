using Soundtrail.Domain.Discovery.Planning;

namespace Soundtrail.Services.Tests.Unit.Orchestrator.OnLookupWorkReady;

public sealed class LookupWorkReadyHandlerTests
{
    [Fact]
    public async Task Given_A_Request_When_Handling_Then_Each_Planned_Lookup_Is_Sent_As_A_Command()
    {
        var environment = LookupWorkReadyHandlerUnitTestEnvironment.Create();
        var subject = environment.CreateSubject();
        var request = LookupWorkReadyHandlerUnitTestEnvironment.CreateStreamingLocationRequest();
        var plan = LookupPlanningPolicy.Build(request);

        await subject.Handle(request);

        environment.CommandBus.Commands.Should().HaveCount(plan.Attempts.Count);
    }
}
