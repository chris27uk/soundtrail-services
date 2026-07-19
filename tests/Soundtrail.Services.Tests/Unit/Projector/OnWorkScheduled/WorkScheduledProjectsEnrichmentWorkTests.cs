using Soundtrail.Domain.Common;
using Soundtrail.Domain.Discovery.Messages;

namespace Soundtrail.Services.Tests.Unit.Projector.OnWorkScheduled;

public sealed class WorkScheduledProjectsEnrichmentWorkTests
{
    [Fact]
    public async Task Given_A_WorkScheduled_Event_When_Projecting_Then_A_DispatchLookupWork_Command_Is_Sent()
    {
        var environment = WorkScheduledProjectorUnitTestEnvironment.Create();
        var subject = environment.CreateSubject();

        await subject.Handle(WorkScheduledProjectorUnitTestEnvironment.CreateEvent());

        environment.CommandBus.Commands.Should().ContainSingle().Which.Should().BeOfType<DispatchLookupWork>();
    }

    [Fact]
    public async Task Given_A_WorkScheduled_Event_When_Projecting_Then_The_Scheduled_Priority_Is_Preserved()
    {
        var environment = WorkScheduledProjectorUnitTestEnvironment.Create();
        var subject = environment.CreateSubject();

        await subject.Handle(WorkScheduledProjectorUnitTestEnvironment.CreateEvent(priority: LookupPriorityBand.Low));

        environment.CommandBus.Commands.Cast<DispatchLookupWork>().Single().Priority.Should().Be(LookupPriorityBand.Low);
    }
}
